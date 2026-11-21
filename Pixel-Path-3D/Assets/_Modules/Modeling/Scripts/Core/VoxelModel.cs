using System.Collections.Generic;
using UnityEngine;

public class VoxelModel : MonoBehaviour
{
    private readonly Dictionary<Vector3Int, Color> voxels = new();
    public int VoxelCount => voxels.Count;

    public event System.Action<Vector3Int, Color> OnVoxelAdded;
    public event System.Action<Vector3Int> OnVoxelRemoved;
    public event System.Action<Vector3Int, Color> OnVoxelColorChanged;
    public event System.Action OnModelCleared;

    // Allows the renderer to read existing voxels.
    public IReadOnlyDictionary<Vector3Int, Color> Voxels => voxels;
    public bool HasVoxel(Vector3Int position) { return voxels.ContainsKey(position); }

    public bool AddVoxel(Vector3Int position, Color color)
    {
        if (voxels.ContainsKey(position)) { Debug.LogWarning($"VoxelModel: Cell {position} is already occupied.", this); return false; }
        voxels.Add(position, color);
        OnVoxelAdded?.Invoke(position, color);
        Debug.Log($"VoxelModel: Added voxel at {position}. Total: {VoxelCount}", this);
        return true;
    }

    public bool RemoveVoxel(Vector3Int position)
    {
        if (!voxels.Remove(position)) { Debug.LogWarning($"VoxelModel: No voxel found at {position}.", this); return false; }
        OnVoxelRemoved?.Invoke(position);
        Debug.Log($"VoxelModel: Removed voxel at {position}. Total: {VoxelCount}", this);
        return true;
    }

    public bool TryGetVoxelColor(Vector3Int position, out Color color) { return voxels.TryGetValue(position, out color); }

    public bool SetVoxelColor(Vector3Int position, Color color)
    {
        if (!voxels.ContainsKey(position)) { Debug.LogWarning($"VoxelModel: Cannot paint empty cell {position}.", this); return false; }
        voxels[position] = color;
        OnVoxelColorChanged?.Invoke(position, color);
        Debug.Log($"VoxelModel: Changed color at {position}.", this);
        return true;
    }

    public void ClearModel()
    {
        voxels.Clear();
        Debug.Log("VoxelModel: Model cleared.", this);
        OnModelCleared?.Invoke();
    }
}
