using System;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelSelectionMath
{
    public static bool TryGetBounds(IEnumerable<Vector3Int> selection, out BoundsInt bounds) {
        bounds = default;

        if (selection == null) return false;

        bool found = false;
        Vector3Int min = default;
        Vector3Int max = default;

        foreach (Vector3Int cell in selection)
        {
            if (!found) { min = max = cell; found = true; }
            else { min = Vector3Int.Min(min, cell); max = Vector3Int.Max(max, cell); }
        }

        if (!found) return false;

        bounds = new BoundsInt(min, max - min + Vector3Int.one);
        return true;
    }

    public static HashSet<Vector3Int> GetSelectionBox(Vector3Int start, Vector3Int end) {
        Vector3Int min = Vector3Int.Min(start, end);
        Vector3Int max = Vector3Int.Max(start, end);

        var result = new HashSet<Vector3Int>();

        for (int z = min.z; z <= max.z; z++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++) { result.Add(new Vector3Int(x, y, z)); }
            }
        }

        return result;
    }

    public static Vector3Int Translate(Vector3Int cell, Vector3Int delta)  { return cell + delta; }

    public static Vector3Int Mirror(Vector3Int cell, Vector3Int pivot, int axis)
    {
        switch (axis)
        {
            case 0:
                return new Vector3Int(2 * pivot.x - cell.x, cell.y, cell.z);
            case 1:
                return new Vector3Int(cell.x, 2 * pivot.y - cell.y, cell.z);
            case 2:
                return new Vector3Int(cell.x, cell.y, 2 * pivot.z - cell.z);
            default:
                throw new ArgumentOutOfRangeException(nameof(axis));
        }
    }

    public static Vector3Int Rotate90(Vector3Int cell, Vector3Int pivot, int axis, bool clockwise = true)
    {
        Vector3Int d = cell - pivot;
        if (clockwise)
        {
            switch (axis)
            {
                case 0:
                    return pivot + new Vector3Int(d.x, -d.z, d.y);
                case 1:
                    return pivot + new Vector3Int(d.z, d.y, -d.x);
                case 2:
                    return pivot + new Vector3Int(-d.y, d.x, d.z);
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis));
            }
        }

        switch (axis)
        {
            case 0:
                return pivot + new Vector3Int(d.x, d.z, -d.y);
            case 1:
                return pivot + new Vector3Int(-d.z, d.y, d.x);
            case 2:
                return pivot + new Vector3Int(d.y, -d.x, d.z);
            default:
                throw new ArgumentOutOfRangeException(nameof(axis));
        }
    }

    public static HashSet<Vector3Int> FloodSelect(VoxelModel model, VoxelGrid grid, Vector3Int start, float tolerance = 0f)
    {
        var result = new HashSet<Vector3Int>();
        if (model == null || grid == null || !grid.IsInsideGrid(start) || !model.TryGetVoxelColor(start, out Color startColor)) { return result; }

        tolerance = Mathf.Max(0f, tolerance);

        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();

        Vector3Int[] neighbors = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, Vector3Int.forward, Vector3Int.back };

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector3Int cell = queue.Dequeue();

            if (!visited.Add(cell)) continue;
            if (!grid.IsInsideGrid(cell)) continue;
            if (!model.TryGetVoxelColor(cell, out Color color)) continue;

            Vector3 difference = new Vector3(color.r - startColor.r, color.g - startColor.g, color.b - startColor.b);

            if (difference.magnitude > tolerance) continue;

            result.Add(cell);
            foreach (Vector3Int direction in neighbors) { queue.Enqueue(cell + direction); }
        }
        return result;
    }
}