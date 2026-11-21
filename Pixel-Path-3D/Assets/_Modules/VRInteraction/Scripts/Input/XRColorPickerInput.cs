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

        Vector3Int cell = voxelGrid.WorldToVoxel(hand.IndexTipPosition);

        bool validTarget = voxelGrid.IsInsideGrid(cell) && voxelModel.HasVoxel(cell);
        if (validTarget) colorPickerTool.UpdateCellTarget(cell);
        else colorPickerTool.ClearTarget();

        if (hand.PinchStarted && validTarget)
        {
            if (colorPickerTool.PickColor()) { Debug.Log($"[XRColorPickerInput] Picked color from {cell}", this); }
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
}