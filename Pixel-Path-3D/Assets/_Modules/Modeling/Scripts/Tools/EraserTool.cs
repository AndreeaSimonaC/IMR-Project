using UnityEngine;

public class EraserTool : MonoBehaviour
{
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelEditOperations editOperations;
    [SerializeField] private Transform voxelRoot;
    [SerializeField] private SelectionManager selectionManager;
    private Vector3Int targetCell;
    private bool hasTarget;
    public bool HasTarget => hasTarget;
    public Vector3Int TargetCell => targetCell;

    private bool CanUseEraser()
    {
        return toolManager != null && toolManager.ActiveTool == HandTool.Eraser && voxelGrid != null && voxelModel != null && editOperations != null;
    }

    public void UpdateTarget(RaycastHit hit)
    {
        ClearTarget();

        if (!CanUseEraser() || voxelRoot == null || hit.collider == null || !hit.collider.transform.IsChildOf(voxelRoot)) { return; }

        Vector3 inward = hit.point - hit.normal * voxelGrid.VoxelSize * 0.02f;
        Vector3Int cell = voxelGrid.WorldToVoxel(inward);
        UpdateCellTarget(cell);
    }

    public void UpdateCellTarget(Vector3Int cell)
    {
        ClearTarget();

        if (!CanUseEraser()) return;
        if (!voxelGrid.IsInsideGrid(cell) || !voxelModel.HasVoxel(cell)) return;

        targetCell = cell;
        hasTarget = true;
    }


    public bool EraseTarget()
    {
        if (!CanUseEraser() || !hasTarget) return false;

        Vector3Int cell = targetCell;
        ClearTarget();

        if (selectionManager != null && selectionManager.IsSelected(cell))
        {
            Debug.Log("[EraserTool] Erasing selected voxel group.");
            return editOperations.EraseSelected();
        }

        return editOperations.EraseCell(cell);
    }


    public bool EraseSelection()
    {
        if (!CanUseEraser()) return false;
        return editOperations.EraseSelected();
    }

    public void ClearTarget() { hasTarget = false; }

    private void OnDisable() { ClearTarget(); }
}