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
        if (gridFloor == null || gridLines == null) { Debug.LogError("VoxelGrid: Floor or lines not assigned!", this); return; }

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

    public float VoxelSize => voxelSize;

    public Vector3Int WorldToVoxel(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        float halfWidth = ProjectSettings.Width * voxelSize / 2f;
        float halfLength = ProjectSettings.Length * voxelSize / 2f;

        int x = Mathf.FloorToInt((localPosition.x + halfWidth) / voxelSize);
        int y = Mathf.FloorToInt(localPosition.y / voxelSize);
        int z = Mathf.FloorToInt((localPosition.z + halfLength) / voxelSize);

        return new Vector3Int(x, y, z);
    }

    public Vector3 VoxelToWorld(Vector3Int voxelPosition)
    {
        float halfWidth = ProjectSettings.Width * voxelSize / 2f;
        float halfLength = ProjectSettings.Length * voxelSize / 2f;
        Vector3 localPosition = new Vector3(-halfWidth + (voxelPosition.x + 0.5f) * voxelSize, (voxelPosition.y + 0.5f) * voxelSize, -halfLength + (voxelPosition.z + 0.5f) * voxelSize);

        return transform.TransformPoint(localPosition);
    }

    public bool IsInsideGrid(Vector3Int voxelPosition) { return voxelPosition.x >= 0 && voxelPosition.x < ProjectSettings.Width && voxelPosition.y >= 0 && voxelPosition.y < ProjectSettings.Height && voxelPosition.z >= 0 && voxelPosition.z < ProjectSettings.Length; }

#if UNITY_EDITOR

    [ContextMenu("TEST - Coordinate Conversion")]
    private void TestCoordinateConversion()
    {
        Vector3Int cell = new Vector3Int(2, 3, 4);

        Vector3 world = VoxelToWorld(cell);
        Vector3Int result = WorldToVoxel(world);

        Debug.Log(
            $"Coordinate test: {cell} -> {world} -> {result} | " +
            $"Passed: {cell == result}",
            this
        );
    }

    [ContextMenu("TEST - Grid Boundaries")]
    private void TestGridBoundaries()
    {
        Vector3Int firstCell = Vector3Int.zero;

        Vector3Int lastCell = new Vector3Int(
            ProjectSettings.Width - 1,
            ProjectSettings.Height - 1,
            ProjectSettings.Length - 1
        );

        Vector3Int outsideCell = new Vector3Int(
            ProjectSettings.Width,
            0,
            0
        );

        Debug.Log(
            $"First cell valid: {IsInsideGrid(firstCell)} | " +
            $"Last cell valid: {IsInsideGrid(lastCell)} | " +
            $"Outside cell invalid: {!IsInsideGrid(outsideCell)}",
            this
        );
    }

    [ContextMenu("TEST - Grid Dimensions")]
    private void TestGridDimensions()
    {
        Debug.Log(
            $"Grid: {ProjectSettings.Width} x " +
            $"{ProjectSettings.Height} x " +
            $"{ProjectSettings.Length} | " +
            $"Voxel size: {voxelSize} | " +
            $"Physical floor: {ProjectSettings.Width * voxelSize} x " +
            $"{ProjectSettings.Length * voxelSize}",
            this
        );
    }

#endif

    private void OnDestroy() { if (generatedMesh != null) Destroy(generatedMesh); }
}
