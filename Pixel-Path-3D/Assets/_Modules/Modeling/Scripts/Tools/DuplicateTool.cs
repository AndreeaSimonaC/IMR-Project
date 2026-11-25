using UnityEngine;

public class DuplicateTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelEditOperations editOperations;

    public bool HasSelection => selectionManager != null && selectionManager.HasSelection;

    private bool CanUseDuplicate() { return isActiveAndEnabled && toolManager != null && toolManager.ActiveTool == HandTool.Duplicate && selectionManager != null && selectionManager.HasSelection && editOperations != null; }

    public bool DuplicateBy(Vector3Int delta)
    {
        if (!CanUseDuplicate() || delta == Vector3Int.zero) return false;

        int originalCount = selectionManager.Count;
        bool duplicated = editOperations.DuplicateSelected(delta);

        if (duplicated) { Debug.Log($"[DuplicateTool] Duplicated {originalCount} voxels by {delta}.", this); }

        return duplicated;
    }
}