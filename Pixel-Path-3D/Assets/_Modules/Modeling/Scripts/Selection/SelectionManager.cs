using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private ToolManager toolManager;
    private readonly HashSet<Vector3Int> selected = new();
    public IReadOnlyCollection<Vector3Int> Selected => selected;
    public bool HasSelection => selected.Count > 0;
    public int Count => selected.Count;
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
            voxelModel.OnModelCleared += ClearSelection;
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
            voxelModel.OnModelCleared -= ClearSelection;
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
        if (selected.Count == 1 && selected.Contains(cell)) return;

        selected.Clear();
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

        if (selected.SetEquals(replacement)) return;

        selected.Clear();
        selected.UnionWith(replacement);
        OnSelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        if (selected.Count == 0) return;

        selected.Clear();
        OnSelectionChanged?.Invoke();
    }

    private void HandleVoxelRemoved(Vector3Int cell)
    {
        RemoveFromSelection(cell);
    }


    [ContextMenu("Debug Selection")]
    private void DebugSelection()
    {
        Debug.Log($"[SelectionManager] Selected voxels: {Count}");

        foreach (Vector3Int cell in selected)
        {
            Debug.Log($"[SelectionManager] Selected cell: {cell}");
        }
    }

}