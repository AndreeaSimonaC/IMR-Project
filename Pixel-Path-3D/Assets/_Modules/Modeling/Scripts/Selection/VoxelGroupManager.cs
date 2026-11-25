using System.Collections.Generic;
using UnityEngine;

public class VoxelGroupManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;

    private readonly Dictionary<int, HashSet<Vector3Int>> groups = new();
    private readonly Dictionary<Vector3Int, int> cellToGroup = new();
    private int nextGroupId = 1;

    public bool TryGetGroupId(Vector3Int cell, out int groupId) { return cellToGroup.TryGetValue(cell, out groupId); }

    public bool TryGetGroupCells(Vector3Int cell, out IReadOnlyCollection<Vector3Int> cells)
    {
        cells = null;
        if (!cellToGroup.TryGetValue(cell, out int groupId)) return false;
        if (!groups.TryGetValue(groupId, out HashSet<Vector3Int> group)) return false;
        cells = group;
        return true;
    }

    public bool CreateGroup(IEnumerable<Vector3Int> selectedCells)
    {
        if (voxelModel == null || selectedCells == null) return false;
        var cells = new HashSet<Vector3Int>(selectedCells);
        if (cells.Count < 2) return false;
        foreach (Vector3Int cell in cells) { if (!voxelModel.HasVoxel(cell) || cellToGroup.ContainsKey(cell)) return false; }
        int id = nextGroupId++;
        groups.Add(id, cells);
        foreach (Vector3Int cell in cells) cellToGroup.Add(cell, id);

        Debug.Log($"[VoxelGroupManager] Created group {id} with {cells.Count} voxels.", this);
        return true;
    }

    public bool Ungroup(Vector3Int cell)
    {
        if (!cellToGroup.TryGetValue(cell, out int id)) return false;
        if (!groups.TryGetValue(id, out HashSet<Vector3Int> cells)) return false;
        foreach (Vector3Int member in cells) cellToGroup.Remove(member);
        groups.Remove(id);
        Debug.Log($"[VoxelGroupManager] Removed group {id}.", this);
        return true;
    }

    public List<HashSet<Vector3Int>> CaptureGroups(IEnumerable<Vector3Int> sourceCells)
    {
        var result = new List<HashSet<Vector3Int>>();
        var capturedIds = new HashSet<int>();
        var source = new HashSet<Vector3Int>(sourceCells);

        foreach (Vector3Int cell in source)
        {
            if (!cellToGroup.TryGetValue(cell, out int groupId)) continue;
            if (!capturedIds.Add(groupId)) continue;
            if (!groups.TryGetValue(groupId, out HashSet<Vector3Int> members)) continue;
            if (source.IsSupersetOf(members)) result.Add(new HashSet<Vector3Int>(members));
        }

        return result;
    }

    public void RestoreGroups(List<HashSet<Vector3Int>> capturedGroups, Dictionary<Vector3Int, List<Vector3Int>> destinationsBySource)
    {
        if (capturedGroups == null || destinationsBySource == null) return;
        foreach (HashSet<Vector3Int> group in capturedGroups)
        {
            var newMembers = new HashSet<Vector3Int>();
            foreach (Vector3Int oldCell in group)
            {
                if (!destinationsBySource.TryGetValue(oldCell, out List<Vector3Int> targets)) continue;
                foreach (Vector3Int target in targets) newMembers.Add(target);
            }

            if (newMembers.Count < 2) continue;
            if (!CreateGroup(newMembers)) Debug.LogWarning("[VoxelGroupManager] Could not restore transformed group.", this);
        }
    }

    private void OnEnable()
    {
        if (voxelModel == null) return;
        voxelModel.OnVoxelRemoved += HandleVoxelRemoved;
        voxelModel.OnModelCleared += ClearGroups;
    }

    private void OnDisable()
    {
        if (voxelModel == null) return;
        voxelModel.OnVoxelRemoved -= HandleVoxelRemoved;
        voxelModel.OnModelCleared -= ClearGroups;
    }

    private void HandleVoxelRemoved(Vector3Int cell)
    {
        if (!cellToGroup.TryGetValue(cell, out int id)) return;
        cellToGroup.Remove(cell);
        if (!groups.TryGetValue(id, out HashSet<Vector3Int> group)) return;
        group.Remove(cell);

        if (group.Count < 2)
        {
            foreach (Vector3Int remaining in group) cellToGroup.Remove(remaining);
            groups.Remove(id);
        }
    }

    private void ClearGroups()
    {
        groups.Clear();
        cellToGroup.Clear();
        nextGroupId = 1;
    }
}