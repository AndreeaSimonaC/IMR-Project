using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XREraserInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private EraserTool eraserTool;
    [SerializeField] private VoxelGrid voxelGrid;

    [Header("Raycast")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField, Min(0.1f)] private float maxDistance = 3f;

    [Header("Direct Erasing")]
    [SerializeField] private bool directErasing = true;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;
    private bool strokeActive;
    private Vector3Int lastStrokeCell;

    private void Start()
    {
        if (coordinator == null || coordinator.RightHand == null || toolManager == null || eraserTool == null || voxelGrid == null || rayOrigin == null)
        {
            Debug.LogError("XREraserInput: Missing references!", this);
            enabled = false;
            return;
        }
        coordinator.RequireFreshPinch();
        Debug.Log("XREraserInput: Initialized.", this);
    }

    private void Update()
    {
        XRHandInput hand = coordinator.RightHand;

        bool available = hand.IsTracked && toolManager.ActiveTool == HandTool.Eraser && !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen();

        if (!available)
        {
            if (wasAvailable) coordinator.RequireFreshPinch();

            wasAvailable = false;
            eraserTool.ClearTarget();
            ResetStroke();
            return;
        }

        wasAvailable = true;

        if (directErasing)
        {
            Vector3Int cell = voxelGrid.WorldToVoxel(hand.IndexTipPosition);
            bool inside = voxelGrid.IsInsideGrid(cell);

            if (inside) eraserTool.UpdateCellTarget(cell);
            else eraserTool.ClearTarget();

            if (!hand.IsPinching || !inside) { ResetStroke(); return; }

            if (!strokeActive)
            {
                EraseCell(cell);
                lastStrokeCell = cell;
                strokeActive = true;
            }
            else if (cell != lastStrokeCell)
            {
                EraseBetween(lastStrokeCell, cell);
                lastStrokeCell = cell;
            }
        }
        else
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayers, QueryTriggerInteraction.Ignore)) eraserTool.UpdateTarget(hit);
            else eraserTool.ClearTarget();
            if (hand.PinchStarted) eraserTool.EraseTarget();
        }
    }

    private void EraseCell(Vector3Int cell)
    {
        if (!voxelGrid.IsInsideGrid(cell)) return;

        eraserTool.UpdateCellTarget(cell);
        if (eraserTool.HasTarget) eraserTool.EraseTarget();
    }

    private void EraseBetween(Vector3Int from, Vector3Int to)
    {
        Vector3Int difference = to - from;
        int steps = Mathf.Max(Mathf.Abs(difference.x), Mathf.Abs(difference.y), Mathf.Abs(difference.z));
        if (steps == 0) return;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3Int cell = Vector3Int.RoundToInt(Vector3.Lerp((Vector3)from, (Vector3)to, t));
            EraseCell(cell);
        }
    }

    private bool IsMenuOpen()
    {
        if (expandedMenus == null) return false;
        foreach (GameObject menu in expandedMenus) { if (menu != null && menu.activeInHierarchy) return true; }

        return false;
    }

    public void SetInteractionBlocked(bool blocked)
    {
        if (interactionBlocked == blocked) return;

        interactionBlocked = blocked;
        coordinator?.RequireFreshPinch();
        ResetStroke();
        eraserTool?.ClearTarget();

        if (blocked) wasAvailable = false;
    }

    public void SetDirectErasing(bool enabled)
    {
        if (directErasing == enabled) return;

        directErasing = enabled;
        coordinator?.RequireFreshPinch();
        ResetStroke();
        eraserTool?.ClearTarget();
    }

    private void ResetStroke() { strokeActive = false; }

    private void OnDisable()
    {
        wasAvailable = false;
        coordinator?.RequireFreshPinch();
        eraserTool?.ClearTarget();
        ResetStroke();
    }
}