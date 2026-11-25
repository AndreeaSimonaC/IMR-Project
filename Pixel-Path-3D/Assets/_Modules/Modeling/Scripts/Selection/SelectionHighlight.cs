using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SelectionHighlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelGrid voxelGrid;

    [Header("Appearance")]
    [SerializeField] private Color highlightColor = new Color(0.86f, 0.17f, 0.57f, 1f);

    [SerializeField, Range(0.01f, 0.15f)]
    private float lineThickness = 0.06f;

    [SerializeField, Range(1f, 1.2f)]
    private float outlineSize = 1.04f;

    private Material highlightMaterial;
    private Transform highlightRoot;

    private static readonly int[,] Edges =
    {
        { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
        { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
        { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
    };

    private void Awake()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("[SelectionHighlight] No compatible shader found.", this);
            enabled = false;
            return;
        }

        highlightMaterial = new Material(shader);

        if (highlightMaterial.HasProperty("_BaseColor")) highlightMaterial.SetColor("_BaseColor", highlightColor);

        if (highlightMaterial.HasProperty("_Color")) highlightMaterial.SetColor("_Color", highlightColor);
    }

    private void OnEnable() { if (selectionManager != null) selectionManager.OnSelectionChanged += RefreshHighlight; }

    private void Start()
    {
        if (selectionManager == null || voxelGrid == null)
        {
            Debug.LogError("[SelectionHighlight] Missing references.", this);
            enabled = false;
            return;
        }

        GameObject root = new GameObject("SelectionHighlights");
        root.transform.SetParent(voxelGrid.transform, false);

        highlightRoot = root.transform;

        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (highlightRoot == null || selectionManager == null) return;

        ClearHighlight();

        var selected = new HashSet<Vector3Int>(selectionManager.Selected);
        var drawnEdges = new HashSet<(Vector3Int, Vector3Int)>();
        foreach (Vector3Int cell in selected) CreateVoxelOutline(cell, selected, drawnEdges);
    }

    private void CreateVoxelOutline(Vector3Int cell, HashSet<Vector3Int> selected, HashSet<(Vector3Int, Vector3Int)> drawnEdges)
    {
        GameObject outline = new GameObject($"Selection_{cell.x}_{cell.y}_{cell.z}");

        outline.transform.SetParent(highlightRoot, false);
        outline.transform.localPosition = voxelGrid.transform.InverseTransformPoint(voxelGrid.VoxelToWorld(cell));

        float halfSize = voxelGrid.VoxelSize * (selected.Count == 1 ? outlineSize : 1f) * 0.5f;

        Vector3[] corners =
        {
            new Vector3(-halfSize, -halfSize, -halfSize),
            new Vector3( halfSize, -halfSize, -halfSize),
            new Vector3( halfSize, -halfSize,  halfSize),
            new Vector3(-halfSize, -halfSize,  halfSize),

            new Vector3(-halfSize,  halfSize, -halfSize),
            new Vector3( halfSize,  halfSize, -halfSize),
            new Vector3( halfSize,  halfSize,  halfSize),
            new Vector3(-halfSize,  halfSize,  halfSize)
        };
        Vector3Int[] gridCorners =
        {
            cell + new Vector3Int(0, 0, 0), cell + new Vector3Int(1, 0, 0), cell + new Vector3Int(1, 0, 1), cell + new Vector3Int(0, 0, 1),
            cell + new Vector3Int(0, 1, 0), cell + new Vector3Int(1, 1, 0),cell + new Vector3Int(1, 1, 1),  cell + new Vector3Int(0, 1, 1)
        };

        for (int i = 0; i < Edges.GetLength(0); i++)
        {
            int aIndex = Edges[i, 0];
            int bIndex = Edges[i, 1];

            Vector3Int start = Vector3Int.Min(gridCorners[aIndex], gridCorners[bIndex]);
            Vector3Int end = Vector3Int.Max(gridCorners[aIndex], gridCorners[bIndex]);

            var edgeKey = (start, end);
            if (!drawnEdges.Add(edgeKey)) continue;
            if (!ShouldDrawEdge(start, end, selected)) continue;

            GameObject edge = new GameObject($"Edge_{i}");
            edge.transform.SetParent(outline.transform, false);
            edge.layer = 2;

            LineRenderer line = edge.AddComponent<LineRenderer>();

            line.sharedMaterial = highlightMaterial;
            line.useWorldSpace = false;
            line.positionCount = 2;

            line.SetPosition(0, corners[aIndex]);
            line.SetPosition(1, corners[bIndex]);

            float width = voxelGrid.VoxelSize * lineThickness;

            line.startWidth = width;
            line.endWidth = width;

            line.startColor = highlightColor;
            line.endColor = highlightColor;

            line.numCapVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
    private bool ShouldDrawEdge(Vector3Int a, Vector3Int b, HashSet<Vector3Int> selected)
    {
        Vector3Int c0, c1, c2, c3;

        if (a.x != b.x)
        {
            c0 = new Vector3Int(a.x, a.y - 1, a.z - 1);
            c1 = new Vector3Int(a.x, a.y - 1, a.z);
            c2 = new Vector3Int(a.x, a.y, a.z - 1);
            c3 = new Vector3Int(a.x, a.y, a.z);
        }
        else if (a.y != b.y)
        {
            c0 = new Vector3Int(a.x - 1, a.y, a.z - 1);
            c1 = new Vector3Int(a.x - 1, a.y, a.z);
            c2 = new Vector3Int(a.x, a.y, a.z - 1);
            c3 = new Vector3Int(a.x, a.y, a.z);
        }
        else
        {
            c0 = new Vector3Int(a.x - 1, a.y - 1, a.z);
            c1 = new Vector3Int(a.x - 1, a.y, a.z);
            c2 = new Vector3Int(a.x, a.y - 1, a.z);
            c3 = new Vector3Int(a.x, a.y, a.z);
        }

        bool p0 = selected.Contains(c0);
        bool p1 = selected.Contains(c1);
        bool p2 = selected.Contains(c2);
        bool p3 = selected.Contains(c3);

        int count = (p0 ? 1 : 0) + (p1 ? 1 : 0) + (p2 ? 1 : 0) + (p3 ? 1 : 0);

        if (count == 1 || count == 3) return true;
        if (count == 2) return (p0 && p3) || (p1 && p2);

        return false;
    }

    private void ClearHighlight()
    {
        if (highlightRoot == null) return;

        for (int i = highlightRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = highlightRoot.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void OnDisable()
    {
        if (selectionManager != null) selectionManager.OnSelectionChanged -= RefreshHighlight;

        ClearHighlight();
    }

    private void OnDestroy()
    {
        if (highlightRoot != null) Destroy(highlightRoot.gameObject);
        if (highlightMaterial != null) Destroy(highlightMaterial);
    }
}