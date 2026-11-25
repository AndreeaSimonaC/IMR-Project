using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRDuplicateInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private DuplicateTool duplicateTool;
    [SerializeField] private VoxelGrid voxelGrid;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;
    private bool hasStartPosition;
    private Vector3 startHandPosition;
    private Vector3Int targetDelta;

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

        Vector3 worldDelta = hand.PinchPosition - startHandPosition;
        Vector3 localDelta = voxelGrid.transform.InverseTransformVector(worldDelta) / voxelGrid.VoxelSize;

        targetDelta = Vector3Int.RoundToInt(localDelta);

        if (hand.PinchStarted && targetDelta != Vector3Int.zero)
        {
            if (duplicateTool.DuplicateBy(targetDelta))
            {
                Debug.Log($"[XRDuplicateInput] Confirmed delta {targetDelta}", this);
                ResetTarget();
                coordinator.RequireFreshPinch();
            }
        }
    }

    private void ResetTarget() { hasStartPosition = false; targetDelta = Vector3Int.zero; }

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