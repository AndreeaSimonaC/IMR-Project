using UnityEngine;

public class DeleteTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelEditOperations editOperations;

    public bool HasSelection => selectionManager != null && selectionManager.HasSelection;

    public bool DeleteSelected()
    {
        if (selectionManager == null || editOperations == null) { Debug.LogWarning("[DeleteTool] Missing references!", this); return false; }
        if (!selectionManager.HasSelection) { Debug.Log("[DeleteTool] Nothing selected.", this); return false; }
        int count = selectionManager.Count;
        bool deleted = editOperations.EraseSelected();
        if (deleted) { Debug.Log($"[DeleteTool] Deleted {count} selected voxels.", this); }

        return deleted;
    }

    public void DeleteSelectedFromButton() { DeleteSelected(); }

}