using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRMoveInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private MoveTool moveTool;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ModelingSettings modelingSettings;
    [SerializeField] private UndoRedoManager undoRedoManager;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private enum DragMode { None, OneHand, TwoHands }
    private DragMode dragMode;
    private bool interactionBlocked;
    private bool wasAvailable;
    private bool waitForRelease;
    private Vector3 startHandPosition;
    private Vector3Int appliedDelta;
    private Vector3 appliedWorldDelta;
    private bool freeDrag;
    private bool ownsTransaction;
    private UndoRedoManager.ObjectTransformSnapshot transformBefore;

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
        appliedWorldDelta = Vector3.zero;
        freeDrag = modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.Free && selectionManager != null && selectionManager.ObjectSelectionCount > 0 && selectionManager.Count == 0;
        BeginHistoryTransaction();
        transformBefore = undoRedoManager != null && selectionManager != null && selectionManager.ObjectSelectionCount > 0 && selectionManager.Count == 0 ? undoRedoManager.CaptureObjectTransforms(selectionManager.SelectedObjects) : null;
    }

    private void UpdateDrag()
    {
        Vector3 worldDelta = GetHandPosition(dragMode) - startHandPosition;
        if (freeDrag)
        {
            Vector3 step = worldDelta - appliedWorldDelta;
            if (step.sqrMagnitude < 0.00000001f) return;
            if (moveTool.MoveObjectsBy(step)) appliedWorldDelta = worldDelta;

            return;
        }

        Vector3 localDelta = voxelGrid.transform.InverseTransformVector(worldDelta) / voxelGrid.VoxelSize;
        Vector3Int targetDelta = Vector3Int.RoundToInt(localDelta);
        Vector3Int snapStep = targetDelta - appliedDelta;

        if (snapStep == Vector3Int.zero) return;
        if (moveTool.MoveBy(snapStep)) appliedDelta = targetDelta;
    }
    private void BeginHistoryTransaction()
    {
        if (undoRedoManager == null || undoRedoManager.IsTransactionActive) return;

        undoRedoManager.BeginTransaction();
        ownsTransaction = undoRedoManager.IsTransactionActive;
    }

    private void FinishHistory()
    {
        if (transformBefore != null && undoRedoManager != null)
            undoRedoManager.RecordObjectTransforms(transformBefore);

        transformBefore = null;
        EndHistoryTransaction();
    }

    private void EndHistoryTransaction()
    {
        if (!ownsTransaction) return;
        ownsTransaction = false;
        if (undoRedoManager != null) undoRedoManager.EndTransaction();
    }

    private void EndDrag()
    {
        FinishHistory();
        dragMode = DragMode.None;
        waitForRelease = true;
        coordinator.RequireFreshPinch();
    }

    private void ResetDrag()
    {
        FinishHistory();
        dragMode = DragMode.None;
        appliedDelta = Vector3Int.zero;
        appliedWorldDelta = Vector3.zero;
        freeDrag = false;
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