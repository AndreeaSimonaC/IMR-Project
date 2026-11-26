using UnityEngine;

public class GroupTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelGroupManager groupManager;
    [SerializeField] private UndoRedoManager undoRedoManager;

    public bool GroupSelected()
    {
        if (selectionManager == null || groupManager == null) { Debug.LogWarning("[GroupTool] Missing references!", this); return false; }
        var before = undoRedoManager != null ? groupManager.CaptureState() : null;
        if (selectionManager.ObjectSelectionCount > 0)
        {
            if (selectionManager.Count > 0) { Debug.LogWarning("[GroupTool] Mixed cell/object selection is not supported.", this); return false; }
            if (selectionManager.ObjectSelectionCount < 2) { Debug.Log("[GroupTool] Select at least 2 objects.", this); return false; }
            bool grouped = groupManager.CreateObjectGroup(selectionManager.SelectedObjects);

            if (grouped)
            {
                if (before != null) undoRedoManager.RecordGroupChanges(groupManager, before);
                Debug.Log($"[GroupTool] Grouped " + $"{selectionManager.ObjectSelectionCount} objects.", this);
            }
            else { Debug.LogWarning("[GroupTool] Could not create object group.", this); }

            return grouped;
        }

        if (selectionManager.Count < 2) { Debug.Log("[GroupTool] Select at least 2 voxels.", this); return false; }
        bool cellGroupCreated = groupManager.CreateGroup(selectionManager.Selected);

        if (cellGroupCreated)
        {
            if (before != null) undoRedoManager.RecordGroupChanges(groupManager, before);
            Debug.Log($"[GroupTool] Grouped {selectionManager.Count} voxels.", this);
        }
        else { Debug.LogWarning("[GroupTool] Could not create group. Check if voxels already belong to another group.", this); }

        return cellGroupCreated;
    }

    public void GroupSelectedFromButton() { GroupSelected(); }
}