#if UNITY_EDITOR
using UnityEngine;

public class VoxelModelTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelGrid voxelGrid;
    private readonly Vector3Int testPosition = new(0, 0, 0);

    [ContextMenu("TEST: Grid Coordinates")]
    private void TestGridCoordinates()
    {
        if (voxelGrid == null) { Debug.LogError("VoxelModelTester: VoxelGrid reference missing", this); return; }

        Vector3Int original = new Vector3Int(5, 0, 8);
        Vector3 worldPosition = voxelGrid.VoxelToWorld(original);
        Vector3Int converted = voxelGrid.WorldToVoxel(worldPosition);

        bool passed = original == converted && voxelGrid.IsInsideGrid(converted);

        Debug.Log($"TEST Grid Coordinates: {(passed ? "Pass" : "Failed")}\n" +
            $"Original: {original}\n" +
            $"World Position: {worldPosition}\n" +
            $"Converted: {converted}",
            this);
    }

    [ContextMenu("TEST: Add Voxel")]
    private void TestAddVoxel()
    {
        if (!ValidateModel() || voxelGrid == null) return;
        if (!voxelGrid.IsInsideGrid(testPosition)) { Debug.LogWarning("TEST AddVoxel: Position outside grid!", this); return; }
        bool success = voxelModel.AddVoxel(testPosition, Color.red);
        Debug.Log($"TEST AddVoxel: {(success ? "Pass" : "Failed")}", this);
    }

    [ContextMenu("TEST: Check Voxel")]
    private void TestHasVoxel()
    {
        if (!ValidateModel()) return;
        bool exists = voxelModel.HasVoxel(testPosition);
        Debug.Log($"TEST HasVoxel: {(exists ? "Pass" : "Failed")} | Exists = {exists}", this);
    }

    [ContextMenu("TEST: Duplicate Voxel")]
    private void TestDuplicateVoxel()
    {
        if (!ValidateModel() || voxelGrid == null) return;
        Vector3Int duplicatePosition = new(1, 0, 0);
        if (!voxelGrid.IsInsideGrid(duplicatePosition))
        { Debug.LogWarning("TEST DuplicateVoxel: Position outside grid!", this); return; }
        if (voxelModel.HasVoxel(duplicatePosition)) { Debug.LogWarning("TEST DuplicateVoxel: Test position already occupied!", this); return; }

        int countBefore = voxelModel.VoxelCount;
        bool first = voxelModel.AddVoxel(duplicatePosition, Color.red);
        bool second = voxelModel.AddVoxel(duplicatePosition, Color.blue);
        bool passed = first && !second && voxelModel.VoxelCount == countBefore + 1;
        Debug.Log($"TEST DuplicateVoxel: {(passed ? "Pass" : "Failed")}", this);

        if (first) voxelModel.RemoveVoxel(duplicatePosition);
    }


    [ContextMenu("TEST: Paint Voxel")]
    private void TestPaintVoxel()
    {
        if (!ValidateModel()) return;
        bool success = voxelModel.SetVoxelColor(testPosition, Color.blue);
        Debug.Log($"TEST PaintVoxel: {(success ? "Pass" : "Failed")}", this);
    }

    [ContextMenu("TEST: Read Color")]
    private void TestReadColor()
    {
        if (!ValidateModel()) return;
        bool found = voxelModel.TryGetVoxelColor(testPosition, out Color color);
        bool passed = found && color == Color.blue;
        Debug.Log($"TEST ReadColor: {(passed ? "Pass" : "Failed")} | Color = {color}", this);
    }

    [ContextMenu("TEST: Remove Voxel")]
    private void TestRemoveVoxel()
    {
        if (!ValidateModel()) return;
        bool success = voxelModel.RemoveVoxel(testPosition);
        Debug.Log($"TEST RemoveVoxel: {(success ? "Pass" : "Failed")}", this);
    }

    private bool ValidateModel()
    {
        if (voxelModel != null) return true;
        Debug.LogError("VoxelModelTester: VoxelModel reference missing", this);
        return false;
    }
}

#endif