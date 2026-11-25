using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRDuplicateInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private DuplicateTool duplicateTool;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ModelingSettings modelingSettings;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;
    private bool hasStartPosition;
    private Vector3 startHandPosition;
    private Vector3Int targetDelta;
    private Vector3 targetWorldDelta;
    private bool IsFreeObjectSelection => modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.Free && selectionManager != null && selectionManager.ObjectSelectionCount > 0 && selectionManager.Count == 0;

    private void Start()
    {
        if (coordinator == null || coordinator.RightHand == null || toolManager == null || duplicateTool == null || voxelGrid == null)
        {
            Debug.LogError("XRDuplicateInput: Missing references!", this );
            enabled = false;
            return;
        }
        coordinator.RequireFreshPinch();
        Debug.Log("XRDuplicateInput: Initialized.", this);
    }

    private void Update()
    {
        XRHandInput hand = coordinator.RightHand;

        bool available = hand.IsTracked && duplicateTool.HasSelection && toolManager.ActiveTool == HandTool.Duplicate && !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen();

        if (!available)
        {
            if (wasAvailable) coordinator.RequireFreshPinch();
            wasAvailable = false;
            ResetTarget();
            return;
        }

        if (!wasAvailable) { coordinator.RequireFreshPinch(); ResetTarget(); }
        wasAvailable = true;

        if (!hasStartPosition)
        {
            if (hand.IsPinching) return;
            startHandPosition = hand.PinchPosition;
            hasStartPosition = true;
            return;
        }

        targetWorldDelta = hand.PinchPosition - startHandPosition;
        Vector3 localDelta = voxelGrid.transform.InverseTransformVector(targetWorldDelta) / voxelGrid.VoxelSize;

        targetDelta = Vector3Int.RoundToInt(localDelta);

        if (!hand.PinchStarted) return;
        bool duplicated;
        if (IsFreeObjectSelection) { duplicated = targetWorldDelta.sqrMagnitude > 0.00000001f && duplicateTool.DuplicateObjectsBy(targetWorldDelta); }
        else { duplicated = targetDelta != Vector3Int.zero && duplicateTool.DuplicateBy(targetDelta); }

        if (duplicated)
        {
            Debug.Log("[XRDuplicateInput] Duplication confirmed.", this);
            ResetTarget();
            coordinator.RequireFreshPinch();
        }
    }

    private void ResetTarget() { hasStartPosition = false; targetDelta = Vector3Int.zero; targetWorldDelta = Vector3.zero; }

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
        wasAvailable = false;
        coordinator?.RequireFreshPinch();
        ResetTarget();
    }

    private void OnDisable()
    {
        wasAvailable = false;
        coordinator?.RequireFreshPinch();
        ResetTarget();
    }
}