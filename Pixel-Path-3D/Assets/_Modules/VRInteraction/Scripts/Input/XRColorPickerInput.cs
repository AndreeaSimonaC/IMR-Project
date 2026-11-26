using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRColorPickerInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private ColorPickerTool colorPickerTool;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelRenderer voxelRenderer;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;

    private void Start()
    {
        if (coordinator == null || coordinator.RightHand == null || toolManager == null || colorPickerTool == null || voxelGrid == null || voxelModel == null)
        {
            Debug.LogError("XRColorPickerInput: Missing references!", this);
            enabled = false;
            return;
        }
        coordinator.RequireFreshPinch();
        Debug.Log("XRColorPickerInput: Initialized.", this);
    }

    private void Update()
    {
        XRHandInput hand = coordinator.RightHand;

        bool available = hand.IsTracked && toolManager.ActiveTool == HandTool.ColorPicker &&
            !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen();

        if (!available)
        {
            if (wasAvailable) coordinator.RequireFreshPinch();
            wasAvailable = false;
            colorPickerTool.ClearTarget();
            return;
        }

        wasAvailable = true;

        bool validTarget = UpdateFingerTarget(hand.IndexTipPosition);

        if (hand.PinchStarted && validTarget)
        {
            if (colorPickerTool.PickColor()) { Debug.Log("[XRColorPickerInput] Color picked.", this); }
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

        if (blocked)
        {
            wasAvailable = false;
            colorPickerTool?.ClearTarget();
        }
    }

    private void OnDisable()
    {
        wasAvailable = false;
        coordinator?.RequireFreshPinch();
        colorPickerTool?.ClearTarget();
    }
    private bool UpdateFingerTarget(Vector3 fingerPosition)
    {
        if (voxelRenderer != null)
        {
            Collider[] hits = Physics.OverlapSphere(fingerPosition, voxelGrid.VoxelSize * 0.45f, ~0, QueryTriggerInteraction.Ignore);
            VoxelObject closestObject = null;
            float closestDistance = float.PositiveInfinity;

            foreach (Collider hit in hits)
            {
                if (!voxelRenderer.TryGetObject(hit.transform, out VoxelObject obj)) continue;
                if (obj == null || obj.Model == null || obj.Model == voxelModel || obj.Model.VoxelCount != 1) continue;
                float distance = (hit.ClosestPoint(fingerPosition) - fingerPosition).sqrMagnitude;

                if (distance < closestDistance) { closestDistance = distance; closestObject = obj; }
            }

            if (closestObject != null)
            {
                foreach (Vector3Int cell in closestObject.Model.Voxels.Keys)
                {
                    colorPickerTool.UpdateObjectTarget(closestObject, cell);
                    return colorPickerTool.HasTarget;
                }
            }
        }

        Vector3Int gridCell = voxelGrid.WorldToVoxel(fingerPosition);

        if (voxelGrid.IsInsideGrid(gridCell) && voxelModel.HasVoxel(gridCell))
        {
            colorPickerTool.UpdateCellTarget(gridCell);
            return colorPickerTool.HasTarget;
        }

        colorPickerTool.ClearTarget();
        return false;
    }
}