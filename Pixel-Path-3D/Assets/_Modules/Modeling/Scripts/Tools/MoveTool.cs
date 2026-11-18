using UnityEngine;

public class MoveTool : MonoBehaviour
{
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelEditOperations editOperations;

    public bool HasSelection => selectionManager != null && selectionManager.HasSelection;

    private bool CanUseMove()
    {
        return toolManager != null && toolManager.ActiveTool == HandTool.Move && selectionManager != null && editOperations != null && selectionManager.HasSelection;
    }

    public bool MoveBy(Vector3Int delta)
    {
        if (!CanUseMove() || delta == Vector3Int.zero) return false;
        bool moved = editOperations.MoveSelected(delta);
        if (moved) { Debug.Log($"[MoveTool] Moved {selectionManager.Count} " + $"voxels by {delta}."); }

        return moved;
    }
    public bool MoveRight() => MoveBy(Vector3Int.right);
    public bool MoveLeft() => MoveBy(Vector3Int.left);
    public bool MoveUp() => MoveBy(Vector3Int.up);
    public bool MoveDown() => MoveBy(Vector3Int.down);
    public bool MoveForward() => MoveBy(Vector3Int.forward);
    public bool MoveBackward() => MoveBy(Vector3Int.back);
}