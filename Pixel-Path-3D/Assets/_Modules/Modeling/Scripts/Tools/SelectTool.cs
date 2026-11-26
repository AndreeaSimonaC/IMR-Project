using UnityEngine;

public class SelectTool : MonoBehaviour
{
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private Transform voxelRoot;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ModelingSettings modelingSettings;
    [SerializeField] private VoxelRenderer voxelRenderer;

    private Vector3Int hoveredCell;
    private bool hasHover;
    private VoxelObject hoveredObject;
    public bool HasTarget => hasHover || hoveredObject != null;
    public Vector3Int TargetCell => hoveredCell;
    public VoxelObject TargetObject => hoveredObject;

    public void UpdateTarget(RaycastHit hit)
    {
        ClearTarget();

        if (!CanUseSelect() || voxelRoot == null || hit.collider == null || !hit.collider.transform.IsChildOf(voxelRoot))
        {
            return;
        }

        if (modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.Free && voxelRenderer != null && voxelRenderer.TryGetObject(hit.collider.transform, out VoxelObject voxelObject))
        {
            UpdateObjectTarget(voxelObject);
            return;
        }

        Vector3 inward = hit.point - hit.normal * voxelGrid.VoxelSize * 0.02f;
        Vector3Int cell = voxelGrid.WorldToVoxel(inward);
        UpdateCellTarget(cell);
    }

    public void UpdateCellTarget(Vector3Int cell)
    {
        ClearTarget();

        if (!CanUseSelect()) return;
        if (!voxelGrid.IsInsideGrid(cell)) return;
        if (!voxelModel.HasVoxel(cell)) return;

        hoveredCell = cell;
        hasHover = true;
    }

    public void SelectTarget(bool additive = false)
    {
        if (!CanUseSelect()) return;

        if (!HasTarget)
        {
            if (!additive) selectionManager.ClearSelection();
            return;
        }

        if (hoveredObject != null)
        {
            if (additive) selectionManager.ToggleSelection(hoveredObject);
            else selectionManager.SelectOnly(hoveredObject);
            Debug.Log($"[SelectTool] Selected objects: " + $"{selectionManager.ObjectSelectionCount}");

            return;
        }

        if (additive) selectionManager.ToggleSelection(hoveredCell);
        else selectionManager.SelectOnly(hoveredCell);

        Debug.Log($"[SelectTool] Selected voxels: {selectionManager.Count}");
    }

    public void ClearTarget() { hasHover = false; hoveredObject = null; }

    private bool CanUseSelect() { return toolManager != null && toolManager.ActiveTool == HandTool.Select && voxelGrid != null && voxelModel != null && selectionManager != null; }

    private void OnDisable() { ClearTarget(); }
    public void UpdateObjectTarget(VoxelObject voxelObject)
    {
        ClearTarget();
        if (!CanUseSelect() || voxelObject == null) return;
        hoveredObject = voxelObject;
    }
}