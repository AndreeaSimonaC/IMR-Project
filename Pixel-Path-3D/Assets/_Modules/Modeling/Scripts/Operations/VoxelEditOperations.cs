using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelEditOperations : MonoBehaviour
{
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelGroupManager groupManager;
    [SerializeField] private ModelingSettings modelingSettings;
    [SerializeField] private UndoRedoManager undoRedoManager;
    private const int MaxScaledVoxels = 4096;

    private bool Ready => voxelModel != null && voxelGrid != null && selectionManager != null;
    private bool SnapEnabled => modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.SnapToGrid;

    public bool EraseCell(Vector3Int cell)
    {
        if (!Ready || !voxelGrid.IsInsideGrid(cell)) return false;
        if (!voxelModel.HasVoxel(cell)) return false;
        var before = undoRedoManager != null ? undoRedoManager.CaptureVoxelState(voxelModel) : null;
        var beforeGroups = before != null && groupManager != null ? groupManager.CaptureState() : null;
        bool erased = voxelModel.RemoveVoxel(cell);

        if (erased)
        {
            if (before != null) undoRedoManager.RecordVoxelChanges(before, groupManager, beforeGroups);
            selectionManager.RemoveFromSelection(cell);
            Debug.Log($"[VoxelEditOperations] Erased voxel {cell}");
        }

        return erased;
    }

    public bool EraseSelected()
    {
        if (!Ready || selectionManager.Count == 0) return false;

        var cells = new List<Vector3Int>(selectionManager.Selected);
        var before = undoRedoManager != null ? undoRedoManager.CaptureVoxelState(voxelModel) : null;
        var beforeGroups = before != null && groupManager != null ? groupManager.CaptureState() : null;
        bool any = false;
        foreach (Vector3Int cell in cells) any |= voxelModel.RemoveVoxel(cell);
        foreach (Vector3Int cell in cells) selectionManager.RemoveFromSelection(cell);

        if (any)
        {
            if (before != null) undoRedoManager.RecordVoxelChanges(before, groupManager, beforeGroups);
            Debug.Log($"[VoxelEditOperations] Erased {cells.Count} voxels.");
        }

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
        if (!Ready || selectionManager.Count == 0) return false;
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
        var before = undoRedoManager != null ? undoRedoManager.CaptureVoxelState(voxelModel) : null;
        var beforeGroups = before != null && groupManager != null ? groupManager.CaptureState() : null;
        ApplyReplacement(source, destination, destinationsBySource);
        if (before != null) undoRedoManager.RecordVoxelChanges(before, groupManager, beforeGroups);
        Debug.Log($"[VoxelEditOperations] Scaled {source.Count} into " + $"{destination.Count} voxels.");

        return true;
    }

    private bool TryTransform(Func<Vector3Int, Vector3Int> map, bool duplicate)
    {
        if (!Ready || selectionManager.Count == 0)
        {
            Debug.LogWarning($"[TRANSFORM DEBUG] Ready={Ready} | " + $"SelectedCells=" + $"{(selectionManager != null ? selectionManager.Count : 0)}");
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
        var before = undoRedoManager != null ? undoRedoManager.CaptureVoxelState(voxelModel) : null;
        var beforeGroups = before != null && groupManager != null ? groupManager.CaptureState() : null;
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
        if (before != null) undoRedoManager.RecordVoxelChanges(before, groupManager, beforeGroups);
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
    private List<VoxelObject> GetSelectedObjects()
    {
        var result = new List<VoxelObject>();
        if (selectionManager == null) return result;
        foreach (VoxelObject obj in selectionManager.SelectedObjects) { if (obj != null) result.Add(obj); }

        return result;
    }

    private Vector3 GetObjectPivot(List<VoxelObject> objects)
    {
        Vector3 center = Vector3.zero;
        foreach (VoxelObject obj in objects) center += obj.transform.position;
        return center / objects.Count;
    }

    public bool MoveSelectedObjects(Vector3 worldDelta)
    {
        if (!Ready || selectionManager.ObjectSelectionCount == 0) return false;
        List<VoxelObject> objects = GetSelectedObjects();
        if (objects.Count == 0) return false;
        if (worldDelta.sqrMagnitude < 0.00000001f) return false;
        bool changed = false;

        if (SnapEnabled)
        {
            float size = voxelGrid.VoxelSize;
            if (size <= 0f) return false;
            Vector3 gridOrigin = voxelGrid.VoxelToWorld(Vector3Int.zero);

            foreach (VoxelObject obj in objects)
            {
                Vector3 targetWorld = obj.transform.position + worldDelta;
                Vector3 localPosition = voxelGrid.transform.InverseTransformVector(targetWorld - gridOrigin);
                Vector3 snappedLocal = new Vector3(Mathf.Round(localPosition.x / size) * size, Mathf.Round(localPosition.y / size) * size, Mathf.Round(localPosition.z / size) * size);
                Vector3 snappedWorld = gridOrigin + voxelGrid.transform.TransformVector(snappedLocal);

                if ((snappedWorld - obj.transform.position).sqrMagnitude < 0.00000001f) continue;

                obj.transform.position = snappedWorld;
                changed = true;
            }

            return changed;
        }
        foreach (VoxelObject obj in objects) { obj.transform.position += worldDelta; changed = true; }
        return changed;
    }

    public bool RotateSelectedObjects(Vector3 worldAxis, float degrees)
    {
        if (!Ready || selectionManager.ObjectSelectionCount == 0) return false;
        if (worldAxis.sqrMagnitude < 0.000001f) return false;
        if (SnapEnabled) degrees = Mathf.Round(degrees / 90f) * 90f;
        if (Mathf.Abs(degrees) < 0.0001f) return false;

        List<VoxelObject> objects = GetSelectedObjects();

        if (objects.Count == 0) return false;

        Vector3 pivot = GetObjectPivot(objects);
        Quaternion rotation = Quaternion.AngleAxis(degrees, worldAxis.normalized);

        foreach (VoxelObject obj in objects)
        {
            Vector3 relativePosition = obj.transform.position - pivot;
            obj.transform.position = pivot + rotation * relativePosition;
            obj.transform.rotation = rotation * obj.transform.rotation;
        }

        return true;
    }

    public bool ScaleSelectedObjects(Vector3 scaleFactors)
    {
        if (!Ready || selectionManager.ObjectSelectionCount == 0) return false;
        if (scaleFactors.x <= 0f || scaleFactors.y <= 0f || scaleFactors.z <= 0f) return false;
        if (SnapEnabled) { scaleFactors = new Vector3(Mathf.Max(0.25f, Mathf.Round(scaleFactors.x * 4f) / 4f), Mathf.Max(0.25f, Mathf.Round(scaleFactors.y * 4f) / 4f), Mathf.Max(0.25f, Mathf.Round(scaleFactors.z * 4f) / 4f)); }
        if (Vector3.Distance(scaleFactors, Vector3.one) < 0.0001f) return false;

        List<VoxelObject> objects = GetSelectedObjects();

        if (objects.Count == 0) return false;

        Vector3 pivot = GetObjectPivot(objects);

        foreach (VoxelObject obj in objects)
        {
            Vector3 current = obj.transform.localScale;
            Vector3 newScale = new Vector3(current.x * scaleFactors.x, current.y * scaleFactors.y, current.z * scaleFactors.z);
            Vector3 relativePosition = obj.transform.position - pivot;
            Vector3 newRelativePosition = new Vector3(relativePosition.x * scaleFactors.x, relativePosition.y * scaleFactors.y, relativePosition.z * scaleFactors.z);

            obj.transform.localScale = newScale;
            obj.transform.position = pivot + newRelativePosition;
        }

        return true;
    }
}