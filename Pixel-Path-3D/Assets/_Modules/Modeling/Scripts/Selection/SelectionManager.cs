using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private VoxelGroupManager groupManager;
    private readonly HashSet<Vector3Int> selected = new();
    private readonly HashSet<VoxelObject> selectedObjects = new();
    public IReadOnlyCollection<Vector3Int> Selected => selected;
    public IReadOnlyCollection<VoxelObject> SelectedObjects => selectedObjects;
    public bool HasSelection => selected.Count > 0 || selectedObjects.Count > 0;
    public int Count => selected.Count;
    public int ObjectSelectionCount => selectedObjects.Count;
    public event Action OnSelectionChanged;

    private void HandleToolChanged(HandTool newTool)
    {
        if (newTool == HandTool.Brush)
        {
            ClearSelection();
            Debug.Log("[SelectionManager] Selection cleared: Brush activated.");
        }
    }

    private void OnEnable()
    {
        if (voxelModel != null)
        {
            voxelModel.OnVoxelRemoved += HandleVoxelRemoved;
            voxelModel.OnModelCleared += ClearGridSelection;
        }
        else
        {
            Debug.LogWarning("[SelectionManager] VoxelModel is not assigned.", this);
        }

        if (toolManager != null) toolManager.OnToolChanged += HandleToolChanged;
    }

    private void OnDisable()
    {
        if (voxelModel != null)
        {
            voxelModel.OnVoxelRemoved -= HandleVoxelRemoved;
            voxelModel.OnModelCleared -= ClearGridSelection;
        }

        if (toolManager != null) toolManager.OnToolChanged -= HandleToolChanged;
    }


    private bool CanSelect(Vector3Int cell)
    {
        return voxelModel != null && voxelModel.HasVoxel(cell);
    }

    public bool IsSelected(Vector3Int cell)
    {
        return selected.Contains(cell);
    }

    public void SelectOnly(Vector3Int cell)
    {
        if (!CanSelect(cell)) return;
        if (selected.Count == 1 && selected.Contains(cell) && selectedObjects.Count == 0) return;

        selected.Clear();
        selectedObjects.Clear();
        selected.Add(cell);
        OnSelectionChanged?.Invoke();
    }

    public void AddToSelection(Vector3Int cell)
    {
        if (!CanSelect(cell)) return;
        if (selected.Add(cell)) OnSelectionChanged?.Invoke();
    }

    public void RemoveFromSelection(Vector3Int cell)
    {
        if (selected.Remove(cell)) OnSelectionChanged?.Invoke();
    }

    public void ToggleSelection(Vector3Int cell)
    {
        if (groupManager != null && groupManager.TryGetGroupCells(cell, out IReadOnlyCollection<Vector3Int> groupCells))
        {
            bool entireGroupSelected = selected.IsSupersetOf(groupCells);
            if (entireGroupSelected)
            {
                foreach (Vector3Int member in groupCells) selected.Remove(member);
                Debug.Log("[SelectionManager] Group deselected.");
            }
            else
            {
                foreach (Vector3Int member in groupCells) { if (CanSelect(member)) selected.Add(member); }
                Debug.Log("[SelectionManager] Entire group selected.");
            }
            OnSelectionChanged?.Invoke();
            return;
        }
        if (selected.Contains(cell))
        {
            selected.Remove(cell);
            OnSelectionChanged?.Invoke();
            return;
        }

        AddToSelection(cell);
    }

    public void ReplaceSelection(IEnumerable<Vector3Int> cells)
    {
        if (cells == null) return;

        var replacement = new HashSet<Vector3Int>();

        foreach (Vector3Int cell in cells)
        {
            if (CanSelect(cell)) replacement.Add(cell);
        }

        if (selected.SetEquals(replacement) && selectedObjects.Count == 0) return;

        selected.Clear();
        selectedObjects.Clear();
        selected.UnionWith(replacement);
        OnSelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        if (selected.Count == 0 && selectedObjects.Count == 0) return;

        selected.Clear();
        selectedObjects.Clear();
        OnSelectionChanged?.Invoke();
    }

    private void HandleVoxelRemoved(Vector3Int cell)
    {
        RemoveFromSelection(cell);
    }


    [ContextMenu("Debug Selection")]
    private void DebugSelection()
    {
        Debug.Log($"[SelectionManager] Selected voxels: {Count}, " + $"selected objects: {ObjectSelectionCount}");

        foreach (Vector3Int cell in selected)
        {
            Debug.Log($"[SelectionManager] Selected cell: {cell}");
        }

        foreach (VoxelObject voxelObject in selectedObjects)
        {
            if (voxelObject != null) { Debug.Log($"[SelectionManager] Selected object: " + $"{voxelObject.name}"); }
        }
    }

    private void ClearGridSelection()
    {
        if (selected.Count == 0) return;
        selected.Clear();
        OnSelectionChanged?.Invoke();
    }
    public bool IsSelected(VoxelObject voxelObject)
    {
        return voxelObject != null && selectedObjects.Contains(voxelObject);
    }

    private HashSet<VoxelObject> GetObjectMembers(VoxelObject voxelObject)
    {
        var result = new HashSet<VoxelObject>();
        if (voxelObject == null) return result;
        if (groupManager != null && groupManager.TryGetGroupObjects(voxelObject, out IReadOnlyCollection<VoxelObject> members))
        {
            foreach (VoxelObject member in members) { if (member != null) result.Add(member); }
        }
        else { result.Add(voxelObject); }

        return result;
    }

    public void SelectOnly(VoxelObject voxelObject)
    {
        if (voxelObject == null) return;
        HashSet<VoxelObject> members = GetObjectMembers(voxelObject);
        if (selected.Count == 0 && selectedObjects.SetEquals(members)) return;

        selected.Clear();
        selectedObjects.Clear();
        selectedObjects.UnionWith(members);

        OnSelectionChanged?.Invoke();
    }

    public void AddToSelection(VoxelObject voxelObject)
    {
        if (voxelObject == null) return;
        HashSet<VoxelObject> members = GetObjectMembers(voxelObject);
        int before = selectedObjects.Count;
        selectedObjects.UnionWith(members);

        if (selectedObjects.Count != before) OnSelectionChanged?.Invoke();
    }

    public void RemoveFromSelection(VoxelObject voxelObject)
    {
        if (voxelObject != null && selectedObjects.Remove(voxelObject)) OnSelectionChanged?.Invoke();
    }

    public void ToggleSelection(VoxelObject voxelObject)
    {
        if (voxelObject == null) return;
        HashSet<VoxelObject> members = GetObjectMembers(voxelObject);
        bool entireGroupSelected = selectedObjects.IsSupersetOf(members);

        if (entireGroupSelected) { selectedObjects.ExceptWith(members); }
        else { selectedObjects.UnionWith(members); }

        OnSelectionChanged?.Invoke();
    }

    public void ReplaceObjectSelection(IEnumerable<VoxelObject> objects)
    {
        if (objects == null) return;
        var replacement = new HashSet<VoxelObject>();
        foreach (VoxelObject voxelObject in objects)
        {
            if (voxelObject == null) continue;
            replacement.UnionWith(GetObjectMembers(voxelObject));
        }

        if (selectedObjects.SetEquals(replacement) && selected.Count == 0) return;

        selected.Clear();
        selectedObjects.Clear();
        selectedObjects.UnionWith(replacement);
        OnSelectionChanged?.Invoke();
    }
}