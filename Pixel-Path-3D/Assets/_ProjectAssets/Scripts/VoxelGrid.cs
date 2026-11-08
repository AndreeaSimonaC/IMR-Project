using System.Collections.Generic;
using UnityEngine;

public class VoxelGrid : MonoBehaviour
{
    [SerializeField] private Transform gridFloor;
    [SerializeField] private MeshFilter gridLines;
    [SerializeField] private PlatformBeamFit platformBeam;
    [SerializeField, Min(0.001f)]
    private float voxelSize = 0.05f;
    [SerializeField, Min(0.001f)]
    private float floorThickness = 0.01f;
    [SerializeField, Min(0.0001f)]
    private float lineThickness = 0.001f;
    private Mesh generatedMesh;

    private void Start()
    {
        if (gridFloor == null || gridLines == null)
        {
            Debug.LogError("VoxelGrid: Floor or lines not assigned!", this);
            return;
        }

        int width = ProjectSettings.Width;
        int length = ProjectSettings.Length;

        float floorWidth = width * voxelSize;
        float floorLength = length * voxelSize;

        gridFloor.localPosition = new Vector3(0, -floorThickness / 2f, 0);
        gridFloor.localRotation = Quaternion.identity;
        gridFloor.localScale = new Vector3(floorWidth, floorThickness, floorLength);

        CreateGridLines(width, length);

        if (platformBeam != null) platformBeam.Fit(floorWidth, floorLength, transform);
        Debug.Log($"VoxelGrid: {width} x {ProjectSettings.Height} x {length}");
    }

    private void CreateGridLines(int width, int length)
    {
        if (generatedMesh != null) Destroy(generatedMesh);

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        float halfWidth = width * voxelSize / 2f;
        float halfLength = length * voxelSize / 2f;
        float y = 0.002f;

        for (int x = 0; x <= width; x++)
        {
            float positionX = -halfWidth + x * voxelSize;
            AddLine(vertices, triangles, new Vector3(positionX, y, -halfLength), new Vector3(positionX, y, halfLength));
        }

        for (int z = 0; z <= length; z++)
        {
            float positionZ = -halfLength + z * voxelSize;
            AddLine(vertices, triangles, new Vector3(-halfWidth, y, positionZ), new Vector3(halfWidth, y, positionZ));
        }

        generatedMesh = new Mesh { name = "Voxel Grid Lines" };
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        gridLines.transform.localPosition = Vector3.zero;
        gridLines.transform.localRotation = Quaternion.identity;
        gridLines.transform.localScale = Vector3.one;
        gridLines.sharedMesh = generatedMesh;
    }

    private void AddLine(List<Vector3> vertices, List<int> triangles, Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, direction) * lineThickness / 2f;

        int index = vertices.Count;

        vertices.Add(start - side);
        vertices.Add(start + side);
        vertices.Add(end + side);
        vertices.Add(end - side);

        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 1);

        triangles.Add(index);
        triangles.Add(index + 3);
        triangles.Add(index + 2);
    }

    private void OnDestroy()
    {
        if (generatedMesh != null) Destroy(generatedMesh);
    }
}
