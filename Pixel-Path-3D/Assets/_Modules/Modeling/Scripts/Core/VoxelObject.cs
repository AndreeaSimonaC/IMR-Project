using UnityEngine;

public class VoxelObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;

    public VoxelModel Model => voxelModel;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
    public Vector3 Scale => transform.localScale;

    public int VoxelCount => voxelModel != null ? voxelModel.VoxelCount : 0;

    public void Initialize(VoxelModel model)
    {
        if (model == null) { Debug.LogError("[VoxelObject] Cannot initialize with a null VoxelModel.", this); return; }
        voxelModel = model;
    }

    public bool TryGetVoxelColor(Vector3Int localCell, out Color color)
    {
        color = default;
        return voxelModel != null && voxelModel.TryGetVoxelColor(localCell, out color);
    }
}