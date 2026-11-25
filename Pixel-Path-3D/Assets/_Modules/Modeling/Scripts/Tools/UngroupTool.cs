using UnityEngine;

public class UngroupTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelGroupManager groupManager;

    public bool UngroupSelected()
    {
        if (selectionManager == null || groupManager == null)
        {
            Debug.LogWarning("[UngroupTool] Missing references!", this);
            return false;
        }

        if (!selectionManager.HasSelection)
        {
            Debug.Log("[UngroupTool] Nothing selected.", this);
            return false;
        }

        int removedGroups = 0;

        foreach (Vector3Int cell in selectionManager.Selected)
        {
            if (!groupManager.TryGetGroupId(cell, out _)) continue;
            if (groupManager.Ungroup(cell)) removedGroups++;
        }

        foreach (VoxelObject obj in selectionManager.SelectedObjects)
        {
            if (obj == null) continue;
            if (!groupManager.TryGetObjectGroupId(obj, out _)) continue;
            if (groupManager.UngroupObject(obj)) removedGroups++;
        }

        if (removedGroups > 0) Debug.Log($"[UngroupTool] Removed {removedGroups} group(s).", this);

        return removedGroups > 0;
    }

    public void UngroupSelectedFromButton() { UngroupSelected(); }
}