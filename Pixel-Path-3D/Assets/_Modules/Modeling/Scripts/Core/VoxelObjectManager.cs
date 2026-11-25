using System.Collections.Generic;
using UnityEngine;

public class VoxelObjectManager : MonoBehaviour
{
    private readonly List<VoxelObject> objects = new();
    public IReadOnlyList<VoxelObject> Objects => objects;
    public VoxelObject ActiveObject { get; private set; }
    public bool RegisterObject(VoxelObject voxelObject)
    {
        if (voxelObject == null || objects.Contains(voxelObject)) return false;
        objects.Add(voxelObject);
        if (ActiveObject == null) ActiveObject = voxelObject;
        return true;
    }

    public bool UnregisterObject(VoxelObject voxelObject)
    {
        if (voxelObject == null || !objects.Remove(voxelObject)) return false;
        if (ActiveObject == voxelObject) ActiveObject = objects.Count > 0 ? objects[0] : null;
        return true;
    }

    public bool SetActiveObject(VoxelObject voxelObject)
    {
        if (voxelObject == null || !objects.Contains(voxelObject)) return false;
        ActiveObject = voxelObject;
        return true;
    }
    public VoxelObject CreateObject(string objectName, Vector3 worldPosition)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(transform, true);
        gameObject.transform.position = worldPosition;

        VoxelModel model = gameObject.AddComponent<VoxelModel>();
        VoxelObject voxelObject = gameObject.AddComponent<VoxelObject>();

        voxelObject.Initialize(model);
        RegisterObject(voxelObject);

        Debug.Log($"[VoxelObjectManager] Created {objectName} at {worldPosition}.", this);

        return voxelObject;
    }

    private void Start()
    {
        VoxelObject[] existingObjects = FindObjectsByType<VoxelObject>(FindObjectsSortMode.None);
        foreach (VoxelObject voxelObject in existingObjects) RegisterObject(voxelObject);
        Debug.Log($"[VoxelObjectManager] Registered {objects.Count} voxel object(s).", this);
    }
}