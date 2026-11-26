using UnityEngine;

public class ScaleTool : MonoBehaviour
{
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelEditOperations editOperations;
    [SerializeField] private UndoRedoManager undoRedoManager;
    public bool HasSelection => selectionManager != null && selectionManager.HasSelection;

    private bool CanUseScale()
    {
        return toolManager != null && toolManager.ActiveTool == HandTool.Scale && selectionManager != null && editOperations != null && selectionManager.HasSelection;
    }

    public bool ScaleBy(Vector3Int factors)
    {
        if (!CanUseScale()) return false;

        if (factors.x < 1 || factors.y < 1 || factors.z < 1 || factors.x > 4 || factors.y > 4 || factors.z > 4 || factors == Vector3Int.one)
        {
            return false;
        }
        if (selectionManager.ObjectSelectionCount > 0) { return ScaleObjectsBy(new Vector3(factors.x, factors.y, factors.z)); }

        bool scaled = editOperations.ScaleSelected(factors);

        if (scaled) { Debug.Log($"[ScaleTool] Scaled selection to " + $"{selectionManager.Count} voxels using factors {factors}."); }

        return scaled;
    }

    public bool ScaleX2() => ScaleBy(new Vector3Int(2, 1, 1));
    public bool ScaleY2() => ScaleBy(new Vector3Int(1, 2, 1));
    public bool ScaleZ2() => ScaleBy(new Vector3Int(1, 1, 2));
    public bool ScaleUniform2() => ScaleBy(new Vector3Int(2, 2, 2));

    public bool ScaleObjectsBy(Vector3 factors, bool recordHistory = false)
    {
        if (!CanUseScale() || selectionManager.ObjectSelectionCount == 0) return false;
        var before = recordHistory && undoRedoManager != null ? undoRedoManager.CaptureObjectTransforms(selectionManager.SelectedObjects) : null;
        bool scaled = editOperations.ScaleSelectedObjects(factors);
        if (scaled)
        {
            if (before != null) undoRedoManager.RecordObjectTransforms(before);
            Debug.Log($"[ScaleTool] Scaled " + $"{selectionManager.ObjectSelectionCount} object(s).", this);
        }

        return scaled;
    }
}