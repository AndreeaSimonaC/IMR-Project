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

    [Header("Rotation")]
    [SerializeField, Range(15f, 90f)] private float rotationThreshold = 60f;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;
    private bool isRotating;

    private Vector3 referenceDirection;

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
            return;
        }

        UpdateRotation(direction);
    }

    private void UpdateRotation(Vector3 direction)
    {
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
            Debug.Log($"[XRRotateInput] Rotated around axis {axis}.", this);
        }
    }

    private void ResetRotation() { isRotating = false; }

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