using UnityEngine;

public class GroupTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelGroupManager groupManager;

    public bool GroupSelected()
    {
        if (selectionManager == null || groupManager == null) { Debug.LogWarning("[GroupTool] Missing references!", this); return false; }
        if (selectionManager.Count < 2) { Debug.Log("[GroupTool] Select at least 2 voxels.", this); return false; }
        bool grouped = groupManager.CreateGroup(selectionManager.Selected);

        if (grouped) { Debug.Log($"[GroupTool] Grouped {selectionManager.Count} voxels.", this); }
        else { Debug.LogWarning("[GroupTool] Could not create group. Check if voxels already belong to another group.", this); }

        return grouped;
    }

    public void GroupSelectedFromButton() { GroupSelected(); }
}