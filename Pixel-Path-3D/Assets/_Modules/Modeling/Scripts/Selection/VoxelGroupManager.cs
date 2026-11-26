using System.Collections.Generic;
using UnityEngine;

public class VoxelGroupManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;

    private readonly Dictionary<int, HashSet<Vector3Int>> groups = new();
    private readonly Dictionary<Vector3Int, int> cellToGroup = new();
    private int nextGroupId = 1;
    private readonly Dictionary<int, HashSet<VoxelObject>> objectGroups = new();
    private readonly Dictionary<VoxelObject, int> objectToGroup = new();
    private int nextObjectGroupId = 1;

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
        voxelModel.OnModelCleared += ClearGridGroups;
    }

    private void OnDisable()
    {
        if (voxelModel == null) return;
        voxelModel.OnVoxelRemoved -= HandleVoxelRemoved;
        voxelModel.OnModelCleared -= ClearGridGroups;
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

    private void ClearGridGroups()
    {
        groups.Clear();
        cellToGroup.Clear();
        nextGroupId = 1;
    }
    public bool TryGetObjectGroupId(VoxelObject voxelObject, out int groupId)
    {
        groupId = 0;
        return voxelObject != null && objectToGroup.TryGetValue(voxelObject, out groupId);
    }

    public bool TryGetGroupObjects(VoxelObject voxelObject, out IReadOnlyCollection<VoxelObject> objects)
    {
        objects = null;
        if (!TryGetObjectGroupId(voxelObject, out int groupId)) return false;
        if (!objectGroups.TryGetValue(groupId, out HashSet<VoxelObject> group)) return false;
        objects = group;
        return true;
    }

    public bool CreateObjectGroup(IEnumerable<VoxelObject> selectedObjects)
    {
        if (selectedObjects == null) return false;
        var objects = new HashSet<VoxelObject>();

        foreach (VoxelObject voxelObject in selectedObjects)
        {
            if (voxelObject == null || objectToGroup.ContainsKey(voxelObject)) return false;
            objects.Add(voxelObject);
        }

        if (objects.Count < 2) return false;
        int id = nextObjectGroupId++;
        objectGroups.Add(id, objects);
        foreach (VoxelObject voxelObject in objects) objectToGroup.Add(voxelObject, id);

        Debug.Log($"[VoxelGroupManager] Created object group {id} with {objects.Count} objects.", this);
        return true;
    }

    public bool UngroupObject(VoxelObject voxelObject)
    {
        if (!TryGetObjectGroupId(voxelObject, out int id)) return false;
        if (!objectGroups.TryGetValue(id, out HashSet<VoxelObject> objects)) return false;
        foreach (VoxelObject member in objects) objectToGroup.Remove(member);
        objectGroups.Remove(id);
        Debug.Log($"[VoxelGroupManager] Removed object group {id}.", this);
        return true;
    }

    public void RemoveObjectFromGroup(VoxelObject voxelObject)
    {
        if (!TryGetObjectGroupId(voxelObject, out int id)) return;
        objectToGroup.Remove(voxelObject);
        if (!objectGroups.TryGetValue(id, out HashSet<VoxelObject> group)) return;
        group.Remove(voxelObject);

        if (group.Count < 2)
        {
            foreach (VoxelObject remaining in group) objectToGroup.Remove(remaining);
            objectGroups.Remove(id);
        }
    }
    public class GroupStateSnapshot
    {
        internal readonly Dictionary<int, HashSet<Vector3Int>> CellGroups = new();
        internal readonly Dictionary<int, HashSet<VoxelObject>> ObjectGroups = new();
        internal int NextCellId;
        internal int NextObjectId;
    }
    public GroupStateSnapshot CaptureState()
    {
        var state = new GroupStateSnapshot();
        foreach (var group in groups) state.CellGroups.Add(group.Key, new HashSet<Vector3Int>(group.Value));
        foreach (var group in objectGroups) state.ObjectGroups.Add(group.Key, new HashSet<VoxelObject>(group.Value));
        state.NextCellId = nextGroupId;
        state.NextObjectId = nextObjectGroupId;
        return state;
    }
    public void RestoreState(GroupStateSnapshot state)
    {
        if (state == null) return;

        foreach (var group in state.CellGroups.Values)
        {
            foreach (Vector3Int cell in group) { if (voxelModel == null || !voxelModel.HasVoxel(cell)) throw new System.InvalidOperationException($"Cannot restore group: missing voxel {cell}."); }
        }

        foreach (var group in state.ObjectGroups.Values)
        {
            foreach (VoxelObject obj in group) { if (obj == null || !obj.gameObject.activeInHierarchy) throw new System.InvalidOperationException("Cannot restore group: missing Free object."); }
        }

        groups.Clear();
        cellToGroup.Clear();
        objectGroups.Clear();
        objectToGroup.Clear();

        foreach (var group in state.CellGroups)
        {
            groups.Add(group.Key, new HashSet<Vector3Int>(group.Value));
            foreach (Vector3Int cell in group.Value) cellToGroup.Add(cell, group.Key);
        }

        foreach (var group in state.ObjectGroups)
        {
            objectGroups.Add(group.Key, new HashSet<VoxelObject>(group.Value));
            foreach (VoxelObject obj in group.Value) objectToGroup.Add(obj, group.Key);
        }

        nextGroupId = state.NextCellId;
        nextObjectGroupId = state.NextObjectId;
    }

    public void ClearAllGroups()
    {
        ClearGridGroups();

        objectGroups.Clear();
        objectToGroup.Clear();
        nextObjectGroupId = 1;
    }
}