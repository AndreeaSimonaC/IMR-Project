using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

public class XRBrushInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private BrushTool brushTool;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private ModelingSettings modelingSettings;
    [SerializeField] private UndoRedoManager undoRedoManager;

    [Header("Right Hand")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private Transform indexTip;
    [SerializeField] private Transform thumbTip;

    [Header("Targeting")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField, Min(0.1f)] private float maxDistance = 3f;

    [Header("Pinch Detection")]
    [SerializeField, Min(0.001f)]
    private float pinchStartDistance = 0.025f;

    [SerializeField, Min(0.001f)]
    private float pinchReleaseDistance = 0.04f;

    [Header("Working Layer")]
    [SerializeField] private bool workingLayerMode;
    [SerializeField, Min(0)] private int workingHeight;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    [Header("Direct Painting")]
    [SerializeField] private bool directPainting = true;

    private readonly List<XRHandSubsystem> handSubsystems = new();
    private XRHandSubsystem handSubsystem;
    private bool wasPinching;
    private bool interactionBlocked;
    private bool strokeReady;
    private bool strokeActive;
    private bool ownsTransaction;
    private Vector3Int lastStrokeCell;
    private bool IsFreeMode => modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.Free;

    private void Start()
    {
        if (toolManager == null || brushTool == null || voxelGrid == null || rayOrigin == null || indexTip == null || thumbTip == null)
        {
            Debug.LogError("XRBrushInput: Missing references!", this);
            enabled = false;
            return;
        }

        if (pinchReleaseDistance <= pinchStartDistance)
        {
            Debug.LogError("XRBrushInput: Release distance must exceed start distance!", this);
            enabled = false;
            return;
        }

        SubsystemManager.GetSubsystems(handSubsystems);
        foreach (XRHandSubsystem subsystem in handSubsystems) { if (subsystem.running) { handSubsystem = subsystem; break; } }
        workingHeight = Mathf.Clamp(workingHeight, 0, ProjectSettings.Height - 1);
        Debug.Log("XRBrushInput: Initialized.", this);
    }
    private void UpdateDirectTarget()
    {
        Vector3 brushPosition = indexTip.position;
        Vector3Int cell = voxelGrid.WorldToVoxel(brushPosition);

        if (!voxelGrid.IsInsideGrid(cell)) { brushTool.ClearTarget(); return; }

        if (IsFreeMode) brushTool.UpdateFreeTarget(brushPosition);
        else brushTool.UpdateFreeTarget(cell);
    }

    private void PaintBetween(Vector3Int from, Vector3Int to)
    {
        Vector3Int difference = to - from;
        int steps = Mathf.Max(Mathf.Abs(difference.x), Mathf.Abs(difference.y), Mathf.Abs(difference.z));
        if (steps == 0) return;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3Int cell = Vector3Int.RoundToInt(Vector3.Lerp((Vector3)from, (Vector3)to, t));
            brushTool.PaintCell(cell);
        }
    }

    private void BeginHistory()
    {
        if (undoRedoManager == null || undoRedoManager.IsTransactionActive) return;
        undoRedoManager.BeginTransaction();
        ownsTransaction = undoRedoManager.IsTransactionActive;
    }

    private void FinishHistory()
    {
        if (!ownsTransaction) return;
        ownsTransaction = false;
        if (undoRedoManager != null) undoRedoManager.EndTransaction();
    }

    private void ResetStroke()
    {
        FinishHistory();
        strokeReady = false;
        strokeActive = false;
    }

    private void Update()
    {
        if (handSubsystem == null || !handSubsystem.running) FindHandSubsystem();
        if (handSubsystem == null || !handSubsystem.running || !handSubsystem.rightHand.isTracked)
        {
            ResetInteraction();
            ResetStroke();
            return;
        }

        float tipDistance = Vector3.Distance(indexTip.position, thumbTip.position);
        bool isPinching = wasPinching ? tipDistance < pinchReleaseDistance : tipDistance <= pinchStartDistance;
        bool pinchStarted = isPinching && !wasPinching;
        wasPinching = isPinching;

        if (toolManager.ActiveTool != HandTool.Brush || interactionBlocked || IsMenuOpen())
        {
            brushTool.ClearTarget();
            ResetStroke();
            return;
        }

        if (!isPinching)
        {
            FinishHistory();
            strokeReady = true;
            strokeActive = false;
        }

        if (pinchStarted && strokeReady) BeginHistory();

        if (directPainting)
        {
            UpdateDirectTarget();

            if (IsFreeMode) { if (pinchStarted && strokeReady) brushTool.PlaceVoxel(); }
            else if (isPinching && strokeReady)
            {
                Vector3Int cell = voxelGrid.WorldToVoxel(indexTip.position);

                if (voxelGrid.IsInsideGrid(cell))
                {
                    if (!strokeActive) { brushTool.PaintCell(cell); lastStrokeCell = cell; strokeActive = true; }
                    else if (cell != lastStrokeCell) { PaintBetween(lastStrokeCell, cell); lastStrokeCell = cell; }
                    UpdateDirectTarget();
                }
                else { strokeActive = false; }
            }
        }
        else
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (workingLayerMode) UpdateWorkingLayerTarget(ray);
            else UpdateSurfaceTarget(ray);
            if (pinchStarted && strokeReady) brushTool.PlaceVoxel();
        }
    }

    private void FindHandSubsystem()
    {
        handSubsystems.Clear();
        SubsystemManager.GetSubsystems(handSubsystems);
        handSubsystem = null;

        foreach (XRHandSubsystem subsystem in handSubsystems) { if (subsystem.running) { handSubsystem = subsystem; break; } }
    }

    private void UpdateSurfaceTarget(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"XR BRUSH HIT: {hit.collider.name} | Point: {hit.point} | Normal: {hit.normal}", this);
            brushTool.UpdateTarget(hit);
        }
        else
        {
            Debug.Log("XR BRUSH: Raycast missed.", this);
            brushTool.ClearTarget();
        }
    }

    private void UpdateWorkingLayerTarget(Ray ray)
    {
        float height = (workingHeight + 0.5f) * voxelGrid.VoxelSize;
        Vector3 center = voxelGrid.transform.TransformPoint(new Vector3(0f, height, 0f));
        Plane plane = new Plane(voxelGrid.transform.up, center);

        if (plane.Raycast(ray, out float distance) && distance >= 0f && distance <= maxDistance)
        {
            Vector3 point = ray.GetPoint(distance);
            if (IsFreeMode) brushTool.UpdateFreeTarget(point);
            else brushTool.UpdateFreeTarget(voxelGrid.WorldToVoxel(point));
        }
        else { brushTool.ClearTarget(); }
    }

    private bool IsMenuOpen()
    {
        if (expandedMenus == null) return false;
        foreach (GameObject menu in expandedMenus) { if (menu != null && menu.activeInHierarchy) return true; }
        return false;
    }

    public void SetInteractionBlocked(bool blocked)
    {
        if (blocked) ResetStroke();
        interactionBlocked = blocked;
        brushTool.ClearTarget();
    }

    public void SetWorkingLayerMode(bool enabled) { workingLayerMode = enabled; brushTool.ClearTarget(); }

    public void SetWorkingHeight(int height)
    {
        workingHeight = Mathf.Clamp(height, 0, ProjectSettings.Height - 1);
        brushTool.ClearTarget();
    }

    public void ChangeWorkingHeight(int amount) { SetWorkingHeight(workingHeight + amount); }

    private void ResetInteraction()
    {
        wasPinching = true;
        if (brushTool != null) brushTool.ClearTarget();
    }

    private void OnDisable() { ResetInteraction(); ResetStroke(); }
}