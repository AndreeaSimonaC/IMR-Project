using UnityEngine;

public class RotateTool : MonoBehaviour
{
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelEditOperations editOperations;

    private bool CanUseRotate() { return toolManager != null && toolManager.ActiveTool == HandTool.Rotate && selectionManager != null && editOperations != null && selectionManager.HasSelection; }

    public bool Rotate90(Vector3Int pivot, int axis, bool clockwise = true)
    {
        if (!CanUseRotate() || axis < 0 || axis > 2) return false;
        bool rotated = editOperations.RotateSelected90(pivot, axis, clockwise);
        if (rotated) { Debug.Log($"[RotateTool] Rotated {selectionManager.Count} " + $"voxels by 90 degrees around axis {axis}."); }

        return rotated;
    }

public bool RotateAroundSelectionMin(int axis, bool clockwise = true)
{
    if (!CanUseRotate())
    {
        Debug.LogWarning(
            $"[ROTATE DEBUG] Tool={toolManager?.ActiveTool} | " +
            $"SelectionManager={selectionManager != null} | " +
            $"EditOperations={editOperations != null} | " +
            $"HasSelection={selectionManager != null && selectionManager.HasSelection}"
        );
        return false;
    }

    if (!VoxelSelectionMath.TryGetBounds(selectionManager.Selected, out BoundsInt bounds))
    {
        Debug.LogWarning("[ROTATE DEBUG] Invalid selection bounds.");
        return false;
    }

    Debug.Log($"[ROTATE DEBUG] Pivot={bounds.min} | Axis={axis} | Count={selectionManager.Count}");

    return Rotate90(bounds.min, axis, clockwise);
}

    public bool RotateX() => RotateAroundSelectionMin(0);
    public bool RotateY() => RotateAroundSelectionMin(1);
    public bool RotateZ() => RotateAroundSelectionMin(2);
}