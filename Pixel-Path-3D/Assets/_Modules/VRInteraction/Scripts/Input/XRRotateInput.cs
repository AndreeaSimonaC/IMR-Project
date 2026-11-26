using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRRotateInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private RotateTool rotateTool;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private ModelingSettings modelingSettings;
    [SerializeField] private UndoRedoManager undoRedoManager;

    [Header("Free Rotation")]
    [SerializeField] private Transform rightWrist;

    [Header("Snap Rotation")]
    [SerializeField, Range(15f, 90f)] private float rotationThreshold = 60f;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;
    private bool isRotating;
    private bool freeRotating;
    private bool ownsTransaction;
    private Vector3 referenceDirection;
    private Quaternion lastHandRotation;
    private UndoRedoManager.ObjectTransformSnapshot transformBefore;
    private bool IsFreeObjectSelection => modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.Free && selectionManager != null && selectionManager.ObjectSelectionCount > 0 && selectionManager.Count == 0;

    private void Start()
    {
        if (coordinator == null || coordinator.RightHand == null || coordinator.LeftHand == null || toolManager == null || rotateTool == null || selectionManager == null || voxelGrid == null)
        {
            Debug.LogError("XRRotateInput: Missing references!", this);
            enabled = false;
            return;
        }

        coordinator.RequireFreshPinch();
        Debug.Log("XRRotateInput: Initialized.", this);
    }

    private void Update()
    {
        bool available = toolManager.ActiveTool == HandTool.Rotate && selectionManager.HasSelection && !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen();

        if (!available)
        {
            if (wasAvailable) coordinator.RequireFreshPinch();

            wasAvailable = false;
            ResetRotation();
            return;
        }

        wasAvailable = true;

        if (IsFreeObjectSelection) UpdateFreeRotation();
        else UpdateSnapRotation();
    }

    private void UpdateFreeRotation()
    {
        XRHandInput hand = coordinator.RightHand;

        if (rightWrist == null || !hand.IsTracked || !hand.IsPinching) { ResetRotation(); return; }
        if (!isRotating || !freeRotating)
        {
            if (!hand.PinchStarted) return;

            ResetRotation();
            lastHandRotation = rightWrist.rotation;
            isRotating = true;
            freeRotating = true;
            CaptureHistory();
            return;
        }

        Quaternion currentRotation = rightWrist.rotation;
        Quaternion delta = currentRotation * Quaternion.Inverse(lastHandRotation);

        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;
        if (Mathf.Abs(angle) < 0.05f || axis.sqrMagnitude < 0.000001f) return;
        if (Mathf.Abs(angle) > 45f) { lastHandRotation = currentRotation; return; }
        if (rotateTool.RotateBy(axis, angle)) lastHandRotation = currentRotation;
    }

    private void UpdateSnapRotation()
    {
        if (freeRotating) ResetRotation();

        if (!coordinator.BothHandsPinching)
        {
            if (isRotating) coordinator.RequireFreshPinch();
            ResetRotation();
            return;
        }

        Vector3 direction = coordinator.RightHand.PinchPosition - coordinator.LeftHand.PinchPosition;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();

        if (!isRotating)
        {
            referenceDirection = direction;
            isRotating = true;
            CaptureHistory();
            return;
        }

        Quaternion rotation = Quaternion.FromToRotation(referenceDirection, direction);
        rotation.ToAngleAxis(out float angle, out Vector3 worldAxis);

        if (angle < rotationThreshold || worldAxis.sqrMagnitude < 0.0001f) return;

        Vector3 localAxis = voxelGrid.transform.InverseTransformDirection(worldAxis);

        int axis = 0;
        float maximum = Mathf.Abs(localAxis.x);

        if (Mathf.Abs(localAxis.y) > maximum)
        {
            axis = 1;
            maximum = Mathf.Abs(localAxis.y);
        }

        if (Mathf.Abs(localAxis.z) > maximum)
            axis = 2;

        float axisSign = axis == 0 ? localAxis.x : axis == 1 ? localAxis.y : localAxis.z;
        bool clockwise = axisSign < 0f;

        if (rotateTool.RotateAroundSelectionMin(axis, clockwise))
        {
            referenceDirection = direction;
            Debug.Log($"[XRRotateInput] Snap rotation axis {axis}.", this);
        }
    }

    private void CaptureHistory()
    {
        BeginHistoryTransaction();
        transformBefore = undoRedoManager != null && selectionManager != null && selectionManager.ObjectSelectionCount > 0 && selectionManager.Count == 0 ? undoRedoManager.CaptureObjectTransforms(selectionManager.SelectedObjects) : null;
    }

    private void BeginHistoryTransaction()
    {
        if (undoRedoManager == null || undoRedoManager.IsTransactionActive) return;
        undoRedoManager.BeginTransaction();
        ownsTransaction = undoRedoManager.IsTransactionActive;
    }

    private void FinishHistory()
    {
        if (transformBefore != null && undoRedoManager != null) undoRedoManager.RecordObjectTransforms(transformBefore);
        transformBefore = null;
        EndHistoryTransaction();
    }

    private void EndHistoryTransaction()
    {
        if (!ownsTransaction) return;
        ownsTransaction = false;
        if (undoRedoManager != null) undoRedoManager.EndTransaction();
    }

    private void ResetRotation()
    {
        FinishHistory();
        isRotating = false;
        freeRotating = false;
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
        ResetRotation();
    }

    private void OnDisable()
    {
        wasAvailable = false;
        coordinator?.RequireFreshPinch();
        ResetRotation();
    }
}