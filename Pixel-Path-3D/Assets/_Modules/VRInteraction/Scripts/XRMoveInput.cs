using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRMoveInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private MoveTool moveTool;
    [SerializeField] private VoxelGrid voxelGrid;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private enum DragMode { None, OneHand, TwoHands }

    private DragMode dragMode;
    private bool interactionBlocked;
    private bool wasAvailable;
    private bool waitForRelease;

    private Vector3 startHandPosition;
    private Vector3Int appliedDelta;

    private void Start()
    {
        if (coordinator == null || coordinator.RightHand == null || coordinator.LeftHand == null || toolManager == null || moveTool == null || voxelGrid == null)
        {
            Debug.LogError("XRMoveInput: Missing references!", this);
            enabled = false;
            return;
        }

        coordinator.RequireFreshPinch();
        Debug.Log("XRMoveInput: Initialized.", this);
    }

    private void Update()
    {
        XRHandInput right = coordinator.RightHand;
        XRHandInput left = coordinator.LeftHand;

        bool available = right.IsTracked && moveTool.HasSelection && toolManager.ActiveTool == HandTool.Move && !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen();

        if (!available)
        {
            if (wasAvailable) coordinator.RequireFreshPinch();

            wasAvailable = false;
            ResetDrag();
            return;
        }

        wasAvailable = true;

        if (waitForRelease)
        {
            if (!right.IsPinching && !left.IsPinching) waitForRelease = false;
            return;
        }

        if (dragMode == DragMode.None)
        {
            if (right.IsPinching && left.IsTracked && left.IsPinching) BeginDrag(DragMode.TwoHands);
            else if (right.PinchStarted) BeginDrag(DragMode.OneHand);

            return;
        }

        if (!right.IsPinching || (dragMode == DragMode.TwoHands && !left.IsPinching))
        {
            EndDrag();
            return;
        }

        if (dragMode == DragMode.OneHand && left.IsPinching)
        {
            EndDrag();
            return;
        }

        UpdateDrag();
    }

    private Vector3 GetHandPosition(DragMode mode)
    {
        if (mode == DragMode.TwoHands) return (coordinator.RightHand.PinchPosition + coordinator.LeftHand.PinchPosition) * 0.5f;

        return coordinator.RightHand.PinchPosition;
    }

    private void BeginDrag(DragMode mode)
    {
        dragMode = mode;
        startHandPosition = GetHandPosition(mode);
        appliedDelta = Vector3Int.zero;
    }

    private void UpdateDrag()
    {
        Vector3 worldDelta = GetHandPosition(dragMode) - startHandPosition;
        Vector3 localDelta = voxelGrid.transform.InverseTransformVector(worldDelta) / voxelGrid.VoxelSize;
        Vector3Int targetDelta = Vector3Int.RoundToInt(localDelta);
        Vector3Int step = targetDelta - appliedDelta;

        if (step == Vector3Int.zero) return;
        if (moveTool.MoveBy(step)) appliedDelta = targetDelta;
    }

    private void EndDrag()
    {
        dragMode = DragMode.None;
        waitForRelease = true;
        coordinator.RequireFreshPinch();
    }

    private void ResetDrag()
    {
        dragMode = DragMode.None;
        appliedDelta = Vector3Int.zero;
        waitForRelease = true;
    }

    private bool IsMenuOpen()
    {
        if (expandedMenus == null) return false;

        foreach (GameObject menu in expandedMenus)
        {
            if (menu != null && menu.activeInHierarchy) return true;
        }

        return false;
    }

    public void SetInteractionBlocked(bool blocked)
    {
        if (interactionBlocked == blocked) return;

        interactionBlocked = blocked;
        coordinator?.RequireFreshPinch();
        ResetDrag();
    }

    private void OnDisable()
    {
        wasAvailable = false;
        coordinator?.RequireFreshPinch();
        ResetDrag();
    }
}