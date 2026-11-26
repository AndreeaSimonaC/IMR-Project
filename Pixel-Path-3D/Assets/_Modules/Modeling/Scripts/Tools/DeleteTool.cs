using System.Collections.Generic;
using UnityEngine;

public class DeleteTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelEditOperations editOperations;
    [SerializeField] private VoxelObjectManager objectManager;
    [SerializeField] private VoxelGroupManager groupManager;
    [SerializeField] private UndoRedoManager undoRedoManager;
    public bool HasSelection => selectionManager != null && selectionManager.HasSelection;

    public bool DeleteSelected()
    {
        if (selectionManager == null || editOperations == null) { Debug.LogWarning("[DeleteTool] Missing references!", this); return false; }
        if (!selectionManager.HasSelection) { Debug.Log("[DeleteTool] Nothing selected.", this); return false; }
        int count = selectionManager.Count;
        int deletedObjects = 0;
        bool deleted = false;

        var objects = new List<VoxelObject>(selectionManager.SelectedObjects);

        if (objects.Count > 0 && (objectManager == null || undoRedoManager == null))
        {
            Debug.LogWarning("[DeleteTool] Object or history manager is not assigned.", this);
            return false;
        }

        bool ownTransaction = undoRedoManager != null && !undoRedoManager.IsTransactionActive;
        if (ownTransaction) undoRedoManager.BeginTransaction();

        try
        {
            foreach (VoxelObject obj in objects)
            {
                if (obj == null) continue;
                if (!undoRedoManager.DeleteFreeObject(objectManager, obj, groupManager, selectionManager)) continue;

                deletedObjects++;
                deleted = true;
            }

            if (count > 0) deleted |= editOperations.EraseSelected();
            else if (deleted) selectionManager.ClearSelection();
        }
        finally { if (ownTransaction) undoRedoManager.EndTransaction(); }
        if (deleted) Debug.Log($"[DeleteTool] Deleted {count} selected voxels " + $"and {deletedObjects} object(s).", this);

        return deleted;
    }

    public void DeleteSelectedFromButton() { DeleteSelected(); }
}