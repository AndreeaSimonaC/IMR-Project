using System.Collections.Generic;
using UnityEngine;

public class EraserTool : MonoBehaviour
{
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelEditOperations editOperations;
    [SerializeField] private Transform voxelRoot;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelRenderer voxelRenderer;
    [SerializeField] private VoxelObjectManager objectManager;
    [SerializeField] private VoxelGroupManager groupManager;
    private Vector3Int targetCell;
    private VoxelObject targetObject;
    private bool hasTarget;
    public bool HasTarget => hasTarget || targetObject != null;
    public Vector3Int TargetCell => targetCell;
    public VoxelObject TargetObject => targetObject;
    [SerializeField] private UndoRedoManager undoRedoManager;
    private bool CanUseEraser()
    {
        return toolManager != null && toolManager.ActiveTool == HandTool.Eraser && voxelGrid != null && voxelModel != null && editOperations != null;
    }

    public void UpdateTarget(RaycastHit hit)
    {
        ClearTarget();

        if (!CanUseEraser() || voxelRoot == null || hit.collider == null || !hit.collider.transform.IsChildOf(voxelRoot)) { return; }
        if (voxelRenderer != null && voxelRenderer.TryGetObject(hit.collider.transform, out VoxelObject voxelObject) && voxelObject != null && voxelObject.Model != voxelModel)
        {
            targetObject = voxelObject;
            return;
        }

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
        if (!CanUseEraser() || !HasTarget) return false;
        if (targetObject != null)
        {
            VoxelObject obj = targetObject;
            ClearTarget();
            if (selectionManager != null && selectionManager.IsSelected(obj)) { return EraseSelection(); }

            return EraseObject(obj);
        }

        Vector3Int cell = targetCell;
        ClearTarget();

        if (selectionManager != null && selectionManager.IsSelected(cell))
        {
            Debug.Log("[EraserTool] Erasing selected voxel group.");
            return EraseSelection();
        }

        return editOperations.EraseCell(cell);
    }


    public bool EraseSelection()
    {
        if (!CanUseEraser() || selectionManager == null) return false;
        bool erased = false;
        bool ownTransaction = undoRedoManager != null && !undoRedoManager.IsTransactionActive;
        if (ownTransaction) undoRedoManager.BeginTransaction();
        try
        {
            var selectedObjects = new List<VoxelObject>(selectionManager.SelectedObjects);
            foreach (VoxelObject obj in selectedObjects) erased |= EraseObject(obj);
            if (selectionManager.Selected.Count > 0) erased |= editOperations.EraseSelected();
        }
        finally { if (ownTransaction) undoRedoManager.EndTransaction(); }

        return erased;
    }

    public void ClearTarget() { hasTarget = false; targetObject = null; }

    private void OnDisable() { ClearTarget(); }
    public void UpdateObjectTarget(VoxelObject obj)
    {
        ClearTarget();
        if (!CanUseEraser() || obj == null) return;
        targetObject = obj;
    }

    private bool EraseObject(VoxelObject obj)
    {
        if (obj == null || objectManager == null) return false;
        if (undoRedoManager == null) { Debug.LogWarning("[EraserTool] UndoRedoManager missing.", this); return false; }

        bool removed = undoRedoManager.DeleteFreeObject(objectManager, obj, groupManager, selectionManager);
        if (removed) Debug.Log($"[EraserTool] Removed object {obj.name}.", this);

        return removed;
    }
}