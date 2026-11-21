using System.Collections.Generic;
using UnityEngine;

public class VoxelRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private GameObject voxelPrefab;
    [SerializeField] private Transform voxelRoot;

    private readonly Dictionary<Vector3Int, GameObject> renderedVoxels = new();

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock propertyBlock;

    private void Awake() { propertyBlock = new MaterialPropertyBlock(); }

    private void OnEnable()
    {
        if (voxelModel == null || voxelGrid == null || voxelPrefab == null || voxelRoot == null)
        {
            Debug.LogError("VoxelRenderer: Missing references!", this);
            enabled = false;
            return;
        }

        voxelModel.OnVoxelAdded += RenderVoxel;
        voxelModel.OnVoxelRemoved += RemoveRenderedVoxel;
        voxelModel.OnVoxelColorChanged += UpdateVoxelColor;
        voxelModel.OnModelCleared += ClearRenderedVoxels;

        foreach (var voxel in voxelModel.Voxels) { RenderVoxel(voxel.Key, voxel.Value);}
    }

    private void OnDisable()
    {
        if (voxelModel == null) return;

        voxelModel.OnVoxelAdded -= RenderVoxel;
        voxelModel.OnVoxelRemoved -= RemoveRenderedVoxel;
        voxelModel.OnVoxelColorChanged -= UpdateVoxelColor;
        voxelModel.OnModelCleared -= ClearRenderedVoxels;

        ClearRenderedVoxels();
    }

    private void RenderVoxel(Vector3Int position, Color color)
    {
        if (!voxelGrid.IsInsideGrid(position)) { Debug.LogWarning($"VoxelRenderer: Cell {position} is outside the grid.", this); return; }
        if (renderedVoxels.ContainsKey(position)) return;

        GameObject voxel = Instantiate(voxelPrefab, voxelRoot);
        voxel.name = $"Voxel_{position.x}_{position.y}_{position.z}";
        voxel.transform.localPosition = voxelRoot.InverseTransformPoint(voxelGrid.VoxelToWorld(position));
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
        Destroy(voxel);
        Debug.Log($"VoxelRenderer: Removed voxel at {position}", this);
    }

    private void UpdateVoxelColor(Vector3Int position, Color color)
    {
        if (!renderedVoxels.TryGetValue(position, out GameObject voxel)) return;
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
}
