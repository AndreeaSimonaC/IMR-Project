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
        if (selectionManager.ObjectSelectionCount > 0) { return RotateBy(GetAxis(axis), clockwise ? 90f : -90f); }
        bool rotated = editOperations.RotateSelected90(pivot, axis, clockwise);
        if (rotated) { Debug.Log($"[RotateTool] Rotated {selectionManager.Count} " + $"voxels by 90 degrees around axis {axis}."); }

        return rotated;
    }

    public bool RotateAroundSelectionMin(int axis, bool clockwise = true)
    {
        if (!CanUseRotate() || axis < 0 || axis > 2) return false;
        if (selectionManager.ObjectSelectionCount > 0) { return RotateBy(GetAxis(axis), clockwise ? 90f : -90f); }
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

    public bool RotateBy(Vector3 worldAxis, float degrees)
    {
        if (!CanUseRotate() || selectionManager.ObjectSelectionCount == 0) return false;
        bool rotated = editOperations.RotateSelectedObjects(worldAxis, degrees);
        if (rotated) { Debug.Log($"[RotateTool] Rotated " + $"{selectionManager.ObjectSelectionCount} object(s).", this); }

        return rotated;
    }

    private Vector3 GetAxis(int axis) { return axis switch { 0 => Vector3.right, 1 => Vector3.up,  _ => Vector3.forward }; }
}