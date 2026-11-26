using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRSelectInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private SelectTool selectTool;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelRenderer voxelRenderer;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;

    private void Start()
    {
        if (coordinator == null || coordinator.RightHand == null || toolManager == null || selectTool == null || voxelGrid == null || voxelModel == null)
        {
            Debug.LogError("XRSelectInput: Missing references!", this);
            enabled = false;
            return;
        }

        coordinator.RequireFreshPinch();
        Debug.Log("XRSelectInput: Initialized.", this);
    }

    private void Update()
    {
        XRHandInput hand = coordinator.RightHand;

        bool available = hand.IsTracked && toolManager.ActiveTool == HandTool.Select && !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen();

        if (!available)
        {
            if (wasAvailable) coordinator.RequireFreshPinch();

            wasAvailable = false;
            selectTool.ClearTarget();
            return;
        }

        wasAvailable = true;

        bool validTarget = UpdateFingerTarget(hand.IndexTipPosition);

        if (hand.PinchStarted && validTarget)
        {
            selectTool.SelectTarget(true);
            Debug.Log("[XRSelectInput] Toggled selection.", this);
        }
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

        if (blocked)
        {
            wasAvailable = false;
            selectTool?.ClearTarget();
        }
    }

    private void OnDisable()
    {
        wasAvailable = false;
        coordinator?.RequireFreshPinch();
        selectTool?.ClearTarget();
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
                if (obj == null || obj.Model == null || obj.Model == voxelModel) continue;
                float distance = (hit.ClosestPoint(fingerPosition) - fingerPosition).sqrMagnitude;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                }
            }

            if (closestObject != null)
            {
                selectTool.UpdateObjectTarget(closestObject);
                return selectTool.HasTarget;
            }
        }

        Vector3Int cell = voxelGrid.WorldToVoxel(fingerPosition);

        if (voxelGrid.IsInsideGrid(cell) && voxelModel.HasVoxel(cell))
        {
            selectTool.UpdateCellTarget(cell);
            return selectTool.HasTarget;
        }

        selectTool.ClearTarget();
        return false;
    }
}