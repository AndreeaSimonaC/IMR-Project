using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private GameObject voxelPrefab;
    [SerializeField] private Transform voxelRoot;
    [SerializeField] private VoxelObject voxelObject;
    [SerializeField] private VoxelObjectManager objectManager;

    private readonly Dictionary<Vector3Int, GameObject> renderedVoxels = new();

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock propertyBlock;
    private Transform objectVisualRoot;
    private Vector3 initialObjectPosition;
    private Quaternion initialObjectRotation;
    private Vector3 initialObjectScale;
    private Vector3 initialRootPosition;
    private Quaternion initialRootRotation;
    private Vector3 initialRootScale;
    private class ObjectVisual
    {
        public VoxelObject Object;
        public VoxelModel Model;
        public Transform Root;
        public readonly Dictionary<Vector3Int, GameObject> Voxels = new();
        public Action<Vector3Int, Color> OnAdded;
        public Action<Vector3Int> OnRemoved;
        public Action<Vector3Int, Color> OnColorChanged;
        public Action OnCleared;
    }
    private readonly Dictionary<VoxelObject, ObjectVisual> objectVisuals = new();

    private void Awake() { propertyBlock = new MaterialPropertyBlock(); }

    private void OnEnable()
    {
        if (voxelModel == null || voxelGrid == null || voxelPrefab == null || voxelRoot == null)
        {
            Debug.LogError("VoxelRenderer: Missing references!", this);
            enabled = false;
            return;
        }

        GameObject root = new GameObject("DefaultVoxelObjectVisualRoot");
        objectVisualRoot = root.transform;
        objectVisualRoot.SetParent(voxelRoot, false);

        initialRootPosition = objectVisualRoot.localPosition;
        initialRootRotation = objectVisualRoot.localRotation;
        initialRootScale = objectVisualRoot.localScale;

        if (voxelObject != null)
        {
            initialObjectPosition = voxelObject.transform.position;
            initialObjectRotation = voxelObject.transform.rotation;
            initialObjectScale = voxelObject.transform.localScale;
        }

        voxelModel.OnVoxelAdded += RenderVoxel;
        voxelModel.OnVoxelRemoved += RemoveRenderedVoxel;
        voxelModel.OnVoxelColorChanged += UpdateVoxelColor;
        voxelModel.OnModelCleared += ClearRenderedVoxels;

        foreach (var voxel in voxelModel.Voxels) { RenderVoxel(voxel.Key, voxel.Value); }
        RegisterExistingObjects();
    }

    private void OnDisable()
    {
        if (voxelModel != null)
        {
            voxelModel.OnVoxelAdded -= RenderVoxel;
            voxelModel.OnVoxelRemoved -= RemoveRenderedVoxel;
            voxelModel.OnVoxelColorChanged -= UpdateVoxelColor;
            voxelModel.OnModelCleared -= ClearRenderedVoxels;
        }

        ClearRenderedVoxels();
        foreach (ObjectVisual visual in objectVisuals.Values) { DestroyObjectVisual(visual); }
        objectVisuals.Clear();
        if (objectVisualRoot != null) Destroy(objectVisualRoot.gameObject);
        objectVisualRoot = null;
    }

    private void RenderVoxel(Vector3Int position, Color color)
    {
        if (!voxelGrid.IsInsideGrid(position)) { Debug.LogWarning($"VoxelRenderer: Cell {position} " + "is outside the grid.", this); return; }
        if (renderedVoxels.ContainsKey(position)) return;

        GameObject voxel = Instantiate(voxelPrefab, objectVisualRoot);
        voxel.name = $"Voxel_{position.x}_{position.y}_{position.z}";
        voxel.transform.localPosition = objectVisualRoot.InverseTransformPoint(voxelGrid.VoxelToWorld(position));
        voxel.transform.localRotation = Quaternion.identity;
        voxel.transform.localScale = Vector3.one * voxelGrid.VoxelSize;
        renderedVoxels.Add(position, voxel);
        ApplyColor(voxel, color);

        Debug.Log($"VoxelRenderer: Rendered voxel at {position}", this);
    }

    private void RemoveRenderedVoxel(Vector3Int position)
    {
        if (!renderedVoxels.TryGetValue(position, out GameObject voxel)) return;
        renderedVoxels.Remove(position);
        if (voxel != null) Destroy(voxel);
        Debug.Log($"VoxelRenderer: Removed voxel at {position}", this);
    }

    private void UpdateVoxelColor(Vector3Int position, Color color)
    {
        if (!renderedVoxels.TryGetValue(position, out GameObject voxel)) return;
        if (voxel == null) return;
        ApplyColor(voxel, color);
        Debug.Log($"VoxelRenderer: Recolored voxel at {position}", this);
    }

    private void ApplyColor(GameObject voxel, Color color)
    {
        MeshRenderer meshRenderer = voxel.GetComponent<MeshRenderer>();

        if (meshRenderer == null) { Debug.LogWarning($"{voxel.name}: MeshRenderer missing!", voxel); return; }

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorID, color);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ClearRenderedVoxels()
    {
        foreach (GameObject voxel in renderedVoxels.Values) { if (voxel != null) Destroy(voxel); }
        renderedVoxels.Clear();
    }
    private void LateUpdate()
    {
        RegisterExistingObjects();
        RemoveUnregisteredObjects();

        UpdateDefaultObjectTransform();

        foreach (ObjectVisual visual in objectVisuals.Values)
        {
            if (visual.Object != null && visual.Root != null) { UpdateObjectTransform(visual); }
        }
    }

    private void RegisterExistingObjects()
    {
        if (objectManager == null) return;

        foreach (VoxelObject obj in objectManager.Objects)
        {
            if (obj == null || obj.Model == null) continue;
            if (obj.Model == voxelModel) continue;
            if (objectVisuals.ContainsKey(obj)) continue;

            RegisterObjectVisual(obj);
        }
    }
    private void RegisterObjectVisual(VoxelObject obj)
    {
        GameObject root = new GameObject($"Visual_{obj.name}");
        root.transform.SetParent(voxelRoot, false);

        ObjectVisual visual = new ObjectVisual { Object = obj, Model = obj.Model, Root = root.transform };
        objectVisuals.Add(obj, visual);

        UpdateObjectTransform(visual);

        visual.OnAdded = (cell, color) => RenderObjectVoxel(visual, cell, color);
        visual.OnRemoved = cell => RemoveObjectVoxel(visual, cell);
        visual.OnColorChanged = (cell, color) => UpdateObjectVoxelColor(visual, cell, color);
        visual.OnCleared = () => ClearObjectVoxels(visual);

        visual.Model.OnVoxelAdded += visual.OnAdded;
        visual.Model.OnVoxelRemoved += visual.OnRemoved;
        visual.Model.OnVoxelColorChanged += visual.OnColorChanged;
        visual.Model.OnModelCleared += visual.OnCleared;

        foreach (var voxel in visual.Model.Voxels) { RenderObjectVoxel(visual, voxel.Key, voxel.Value); }

        Debug.Log($"[VoxelRenderer] Registered visual for {obj.name}.", this);
    }

    private void UpdateObjectTransform(ObjectVisual visual)
    {
        visual.Root.position = visual.Object.transform.position;
        visual.Root.rotation = visual.Object.transform.rotation * voxelGrid.transform.rotation;
        visual.Root.localScale = visual.Object.transform.localScale;
    }

    private void RenderObjectVoxel(ObjectVisual visual, Vector3Int cell, Color color)
    {
        if (visual.Root == null || visual.Voxels.ContainsKey(cell)) return;
        if (!voxelGrid.IsInsideGrid(cell)) { Debug.LogWarning($"[VoxelRenderer] Local cell {cell} " + "is outside grid bounds.", this); return; }

        GameObject voxel = Instantiate(voxelPrefab, visual.Root);
        voxel.name = $"Voxel_{cell.x}_{cell.y}_{cell.z}";
        Vector3 originWorld = voxelGrid.VoxelToWorld(Vector3Int.zero);
        Vector3 cellWorld = voxelGrid.VoxelToWorld(cell);

        voxel.transform.localPosition = voxelGrid.transform.InverseTransformVector(cellWorld - originWorld);
        voxel.transform.localRotation = Quaternion.identity;
        voxel.transform.localScale = Vector3.one * voxelGrid.VoxelSize;
        visual.Voxels.Add(cell, voxel);
        ApplyColor(voxel, color);
    }

    private void RemoveObjectVoxel(ObjectVisual visual, Vector3Int cell)
    {
        if (!visual.Voxels.TryGetValue(cell, out GameObject voxel)) return;
        visual.Voxels.Remove(cell);
        if (voxel != null) Destroy(voxel);
    }

    private void UpdateObjectVoxelColor(ObjectVisual visual, Vector3Int cell, Color color)
    {
        if (!visual.Voxels.TryGetValue(cell, out GameObject voxel)) return;
        if (voxel != null) ApplyColor(voxel, color);
    }

    private void ClearObjectVoxels(ObjectVisual visual)
    {
        foreach (GameObject voxel in visual.Voxels.Values) { if (voxel != null) Destroy(voxel); }
        visual.Voxels.Clear();
    }

    private bool IsRegisteredObject(VoxelObject obj)
    {
        if (objectManager == null) return false;
        foreach (VoxelObject registered in objectManager.Objects) { if (registered == obj) return true; }

        return false;
    }

    private void RemoveUnregisteredObjects()
    {
        if (objectManager == null || objectVisuals.Count == 0) return;
        var removed = new List<VoxelObject>();
        foreach (var entry in objectVisuals)
        {
            if (entry.Key == null || !IsRegisteredObject(entry.Key)) { removed.Add(entry.Key); }
        }

        foreach (VoxelObject obj in removed)
        {
            ObjectVisual visual = objectVisuals[obj];
            DestroyObjectVisual(visual);
            objectVisuals.Remove(obj);
        }
    }

    private void DestroyObjectVisual(ObjectVisual visual)
    {
        if (visual.Model != null)
        {
            visual.Model.OnVoxelAdded -= visual.OnAdded;
            visual.Model.OnVoxelRemoved -= visual.OnRemoved;
            visual.Model.OnVoxelColorChanged -= visual.OnColorChanged;
            visual.Model.OnModelCleared -= visual.OnCleared;
        }

        ClearObjectVoxels(visual);

        if (visual.Root != null) Destroy(visual.Root.gameObject);
    }

    public bool TryGetObject(Transform hitTransform, out VoxelObject obj)
    {
        obj = null;

        if (hitTransform == null) return false;

        foreach (var entry in objectVisuals)
        {
            if (entry.Key != null && entry.Value.Root != null && hitTransform.IsChildOf(entry.Value.Root)) { obj = entry.Key; return true; }
        }

        if (objectVisualRoot != null && hitTransform.IsChildOf(objectVisualRoot))
        {
            if (voxelObject != null) { obj = voxelObject; return true; }

            if (objectManager != null)
            {
                foreach (VoxelObject candidate in objectManager.Objects)
                {
                    if (candidate != null && candidate.Model == voxelModel) { obj = candidate; return true; }
                }
            }
        }

        return false;
    }
    public bool TryGetObjectVisualRoot(VoxelObject obj, out Transform visualRoot)
    {
        visualRoot = null;

        if (obj == null) return false;

        if (objectVisuals.TryGetValue(obj, out ObjectVisual visual) && visual.Root != null)
        {
            visualRoot = visual.Root;
            return true;
        }

        return false;
    }

    private void UpdateDefaultObjectTransform()
    {
        if (voxelObject == null || objectVisualRoot == null) return;
        Vector3 scale = voxelObject.transform.localScale;
        if (Mathf.Approximately(initialObjectScale.x, 0f) || Mathf.Approximately(initialObjectScale.y, 0f) || Mathf.Approximately(initialObjectScale.z, 0f)) return;

        Vector3 scaleRatio = new Vector3(scale.x / initialObjectScale.x, scale.y / initialObjectScale.y, scale.z / initialObjectScale.z);

        Quaternion rotationDelta = voxelObject.transform.rotation * Quaternion.Inverse(initialObjectRotation);
        Vector3 positionDelta = voxelObject.transform.position - initialObjectPosition;
        Transform parent = objectVisualRoot.parent;
        Vector3 basePosition = parent.TransformPoint(initialRootPosition);
        Quaternion baseRotation = parent.rotation * initialRootRotation;

        Vector3 pivot = voxelGrid.transform.position;
        Vector3 offset = basePosition - pivot;
        Vector3 scaledOffset = Vector3.Scale(offset, scaleRatio);

        objectVisualRoot.position = pivot + positionDelta + rotationDelta * scaledOffset;
        objectVisualRoot.rotation = rotationDelta * baseRotation;
        objectVisualRoot.localScale = Vector3.Scale(initialRootScale, scaleRatio);
    }
}