using UnityEngine;

[DefaultExecutionOrder(-150)]
public class XRWorkspaceRotateInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private Transform modelRotationPivot;
    [SerializeField] private Collider gridFloorCollider;
    [SerializeField] private VoxelObjectManager objectManager;
    [SerializeField] private VoxelModel defaultVoxelModel;

    [Header("Left Hand Pointing")]
    [SerializeField] private Transform leftWrist;
    [SerializeField] private Transform leftIndexTip;
    [SerializeField] private Transform headCamera;

    [Header("Rotation")]
    [SerializeField, Min(0.1f)]
    private float maxPointingDistance = 3f;
    [SerializeField, Min(1f)]
    private float degreesPerMeter = 180f;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool rotating;
    private bool interactionBlocked;
    private Vector3 lastHandPosition;

    private void Start()
    {
        if (coordinator == null || coordinator.LeftHand == null || coordinator.RightHand == null ||
            toolManager == null || modelRotationPivot == null || gridFloorCollider == null ||
            objectManager == null || defaultVoxelModel == null || leftWrist == null || leftIndexTip == null)
        {
            Debug.LogError("[XRWorkspaceRotateInput] Missing references!", this);
            enabled = false;
            return;
        }

        if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;

        Debug.Log("[XRWorkspaceRotateInput] Initialized.", this);
    }

    private void Update()
    {
        XRHandInput left = coordinator.LeftHand;
        XRHandInput right = coordinator.RightHand;

        bool available = left.IsTracked && !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen() && toolManager.ActiveTool != HandTool.Rotate && toolManager.ActiveTool != HandTool.Scale;
        if (!available || !left.IsPinching || right.IsPinching) { StopRotation(); return; }

        if (!rotating)
        {
            if (!left.PinchStarted) return;
            if (!IsPointingAtGrid()) return;

            rotating = true;
            lastHandPosition = left.PinchPosition;

            Debug.Log("[XRWorkspaceRotateInput] Rotation started.", this);

            return;
        }

        Vector3 currentHandPosition = left.PinchPosition;
        Vector3 cameraRight = headCamera != null ? headCamera.right : Vector3.right;
        cameraRight = Vector3.ProjectOnPlane(cameraRight, Vector3.up);
        if (cameraRight.sqrMagnitude < 0.0001f) cameraRight = Vector3.right;
        cameraRight.Normalize();

        Vector3 handDelta = currentHandPosition - lastHandPosition;

        float horizontalDelta = Vector3.Dot(handDelta, cameraRight);
        float angle = horizontalDelta * degreesPerMeter;
        if (Mathf.Abs(angle) < 0.01f) return;
        angle = Mathf.Clamp(angle, -15f, 15f);
        RotateWorkspace(angle);

        lastHandPosition = currentHandPosition;
    }

    private bool IsPointingAtGrid()
    {
        Vector3 direction = leftIndexTip.position - leftWrist.position;
        if (direction.sqrMagnitude < 0.0001f) return false;
        Ray ray = new Ray(leftIndexTip.position, direction.normalized);

        return gridFloorCollider.Raycast(ray, out _, maxPointingDistance);
    }

    private void RotateWorkspace(float angle)
    {
        Vector3 pivot = modelRotationPivot.position;
        Quaternion delta = Quaternion.AngleAxis(angle, Vector3.up);

        modelRotationPivot.rotation = delta * modelRotationPivot.rotation;

        foreach (VoxelObject obj in objectManager.Objects)
        {
            if (obj == null || obj.Model == null || obj.Model == defaultVoxelModel) { continue; }
            Vector3 relative = obj.transform.position - pivot;

            obj.transform.position = pivot + delta * relative;
            obj.transform.rotation = delta * obj.transform.rotation * Quaternion.Inverse(delta);
        }
    }

    private void StopRotation() { rotating = false; }

    private bool IsMenuOpen()
    {
        if (expandedMenus == null) return false;
        foreach (GameObject menu in expandedMenus) { if (menu != null && menu.activeInHierarchy) return true; }

        return false;
    }

    public void SetInteractionBlocked(bool blocked)
    {
        interactionBlocked = blocked;
        if (blocked) StopRotation();
    }

    private void OnDisable() { StopRotation(); }
}