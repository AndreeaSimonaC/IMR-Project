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

        foreach (Vector3Int cell in selectionManager.Selected) CreateVoxelOutline(cell);
    }

    private void CreateVoxelOutline(Vector3Int cell)
    {
        GameObject outline = new GameObject($"Selection_{cell.x}_{cell.y}_{cell.z}");

        outline.transform.SetParent(highlightRoot, false);
        outline.transform.localPosition = voxelGrid.transform.InverseTransformPoint(voxelGrid.VoxelToWorld(cell));

        float halfSize = voxelGrid.VoxelSize * outlineSize * 0.5f;

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

        for (int i = 0; i < Edges.GetLength(0); i++)
        {
            GameObject edge = new GameObject($"Edge_{i}");
            edge.transform.SetParent(outline.transform, false);
            edge.layer = 2;

            LineRenderer line = edge.AddComponent<LineRenderer>();

            line.sharedMaterial = highlightMaterial;
            line.useWorldSpace = false;
            line.positionCount = 2;

            line.SetPosition(0, corners[Edges[i, 0]]);
            line.SetPosition(1, corners[Edges[i, 1]]);

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