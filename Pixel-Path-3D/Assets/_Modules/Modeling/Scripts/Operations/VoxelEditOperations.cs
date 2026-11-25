using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelEditOperations : MonoBehaviour
{
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelGroupManager groupManager;

    private const int MaxScaledVoxels = 4096;

    private bool Ready => voxelModel != null && voxelGrid != null && selectionManager != null;

    public bool EraseCell(Vector3Int cell)
    {
        if (!Ready || !voxelGrid.IsInsideGrid(cell)) return false;
        bool erased = voxelModel.RemoveVoxel(cell);
        if (erased) { selectionManager.RemoveFromSelection(cell); Debug.Log($"[VoxelEditOperations] Erased voxel {cell}"); }

        return erased;
    }

    public bool EraseSelected()
    {
        if (!Ready || !selectionManager.HasSelection) return false;

        var cells = new List<Vector3Int>(selectionManager.Selected);
        bool any = false;
        foreach (Vector3Int cell in cells) any |= voxelModel.RemoveVoxel(cell);

        selectionManager.ClearSelection();

        if (any) Debug.Log($"[VoxelEditOperations] Erased {cells.Count} voxels.");

        return any;
    }

    public bool MoveSelected(Vector3Int delta)
    {
        if (delta == Vector3Int.zero) return false;
        return TryTransform(cell => cell + delta, false);
    }

    public bool DuplicateSelected(Vector3Int delta)
    {
        if (delta == Vector3Int.zero) return false;
        return TryTransform(cell => cell + delta, true);
    }

    public bool RotateSelected90(Vector3Int pivot, int axis, bool clockwise = true)
    {
        if (axis < 0 || axis > 2) return false;
        return TryTransform(cell => VoxelSelectionMath.Rotate90(cell, pivot, axis, clockwise), false);
    }

    public bool FlipSelected(Vector3Int pivot, int axis)
    {
        if (axis < 0 || axis > 2) return false;
        return TryTransform(cell => VoxelSelectionMath.Mirror(cell, pivot, axis), false);
    }

    public bool ScaleSelected(Vector3Int factors)
    {
        if (!Ready || !selectionManager.HasSelection) return false;
        if (factors.x < 1 || factors.y < 1 || factors.z < 1 || factors.x > 4 || factors.y > 4 || factors.z > 4) return false;
        if (factors == Vector3Int.one) return false;
        if (!VoxelSelectionMath.TryGetBounds(selectionManager.Selected, out BoundsInt bounds)) return false;

        var source = new Dictionary<Vector3Int, Color>();

        foreach (Vector3Int cell in selectionManager.Selected)
        {
            if (!voxelModel.TryGetVoxelColor(cell, out Color color)) return false;
            source.Add(cell, color);
        }

        long requested = (long)source.Count * factors.x * factors.y * factors.z;

        if (requested > MaxScaledVoxels) return false;

        var destination = new Dictionary<Vector3Int, Color>();
        var destinationsBySource = new Dictionary<Vector3Int, List<Vector3Int>>();
        foreach (var entry in source)
        {
            destinationsBySource[entry.Key] = new List<Vector3Int>();
            Vector3Int relative = entry.Key - bounds.min;
            Vector3Int baseCell = bounds.min + new Vector3Int(relative.x * factors.x, relative.y * factors.y, relative.z * factors.z);

            for (int z = 0; z < factors.z; z++)
            {
                for (int y = 0; y < factors.y; y++)
                {
                    for (int x = 0; x < factors.x; x++)
                    {
                        Vector3Int target = baseCell + new Vector3Int(x, y, z);

                        if (!voxelGrid.IsInsideGrid(target)) return false;
                        if (voxelModel.HasVoxel(target) && !source.ContainsKey(target)) return false;
                        if (destination.ContainsKey(target)) return false;

                        destination.Add(target, entry.Value);
                        destinationsBySource[entry.Key].Add(target);
                    }
                }
            }
        }
        ApplyReplacement(source, destination, destinationsBySource);
        Debug.Log($"[VoxelEditOperations] Scaled {source.Count} into " + $"{destination.Count} voxels.");

        return true;
    }

    private bool TryTransform(Func<Vector3Int, Vector3Int> map, bool duplicate)
    {
        if (!Ready || !selectionManager.HasSelection)
        {
            Debug.LogWarning($"[TRANSFORM DEBUG] Ready={Ready} | HasSelection={selectionManager != null && selectionManager.HasSelection}");
            return false;
        }

        var source = new Dictionary<Vector3Int, Color>();

        foreach (Vector3Int cell in selectionManager.Selected)
        {
            if (!voxelModel.TryGetVoxelColor(cell, out Color color))
            {
                Debug.LogWarning($"[TRANSFORM DEBUG] Missing source voxel: {cell}");
                return false;
            }

            source.Add(cell, color);
        }

        var destination = new Dictionary<Vector3Int, Color>();

        foreach (var entry in source)
        {
            Vector3Int target = map(entry.Key);

            if (!voxelGrid.IsInsideGrid(target))
            {
                Debug.LogWarning($"[TRANSFORM DEBUG] OUT OF GRID: {entry.Key} -> {target}");
                return false;
            }

            if (destination.ContainsKey(target))
            {
                Debug.LogWarning($"[TRANSFORM DEBUG] DUPLICATE TARGET: {target}");
                return false;
            }

            if (voxelModel.HasVoxel(target) && (duplicate || !source.ContainsKey(target)))
            {
                Debug.LogWarning($"[TRANSFORM DEBUG] COLLISION: {entry.Key} -> {target}");
                return false;
            }

            destination.Add(target, entry.Value);
        }

        if (duplicate)
        {
            foreach (var entry in destination) voxelModel.AddVoxel(entry.Key, entry.Value);
            selectionManager.ReplaceSelection(destination.Keys);
        }
        else
        {
            var destinationsBySource = new Dictionary<Vector3Int, List<Vector3Int>>();
            foreach (Vector3Int cell in source.Keys) destinationsBySource[cell] = new List<Vector3Int> { map(cell) };
            ApplyReplacement(source, destination, destinationsBySource);
        }

        Debug.Log($"[VoxelEditOperations] {(duplicate ? "Duplicated" : "Transformed")} {source.Count} voxels.");
        return true;
    }
    private void ApplyReplacement(Dictionary<Vector3Int, Color> source, Dictionary<Vector3Int, Color> destination, Dictionary<Vector3Int, List<Vector3Int>> destinationsBySource)
    {
        var capturedGroups = groupManager != null ? groupManager.CaptureGroups(source.Keys) : null;
        foreach (Vector3Int cell in source.Keys) voxelModel.RemoveVoxel(cell);
        foreach (var entry in destination) voxelModel.AddVoxel(entry.Key, entry.Value);
        if (groupManager != null && capturedGroups != null) groupManager.RestoreGroups(capturedGroups, destinationsBySource);
        selectionManager.ReplaceSelection(destination.Keys);
    }
}