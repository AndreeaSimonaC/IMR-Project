using UnityEngine;

public class BrushTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private Collider gridFloorCollider;
    [SerializeField] private Transform voxelRoot;
    [SerializeField] private GameObject previewPrefab;

    [Header("Brush Settings")]
    [SerializeField] private Color brushColor = Color.white;

    private GameObject preview;
    private Vector3Int targetCell;
    private bool hasValidTarget;
    private bool isInitialized;

    private void Awake()
    {
        if (toolManager == null || voxelGrid == null || voxelModel == null || gridFloorCollider == null || voxelRoot == null || previewPrefab == null)
        {
            Debug.LogError("BrushTool: Missing references!", this);
            enabled = false;
            return;
        }

        if (!gridFloorCollider.enabled) { Debug.LogWarning("BrushTool: Grid floor collider is disabled!", this); }

        if (previewPrefab.GetComponentInChildren<Renderer>(true) == null)
        {
            Debug.LogError("BrushTool: Preview prefab has no Renderer!", this);
            enabled = false;
            return;
        }

        preview = Instantiate(previewPrefab, voxelGrid.transform);
        preview.name = "VoxelPlacementPreview";
        foreach (Collider col in preview.GetComponentsInChildren<Collider>(true)) { col.enabled = false; }

        preview.SetActive(false);
        isInitialized = true;

        Debug.Log("BrushTool: Initialized successfully.", this);
    }

    private void OnEnable() { if (toolManager != null) toolManager.OnToolChanged += HandleToolChanged; }

    private void OnDisable() { if (toolManager != null) toolManager.OnToolChanged -= HandleToolChanged; ClearTarget(); }

    private void HandleToolChanged(HandTool tool)
    {
        Debug.Log($"BrushTool: Tool changed to {tool}.", this);
        if (tool != HandTool.Brush) ClearTarget();
    }
    public void UpdateTarget(RaycastHit hit)
    {
        if (!CanUseBrush()) return;
        if (hit.collider == null) { ClearTarget(); return; }

        bool isFloor = hit.collider == gridFloorCollider;
        bool isVoxel = hit.collider.transform.IsChildOf(voxelRoot);

        if (!isFloor && !isVoxel) { ClearTarget(); return; }

        if (isFloor)
        {
            if (Vector3.Dot(hit.normal, voxelGrid.transform.up) < 0.9f) { ClearTarget(); return; }

            Vector3 targetPoint = hit.point + voxelGrid.transform.up * voxelGrid.VoxelSize * 0.02f;
            SetPreviewTarget(voxelGrid.WorldToVoxel(targetPoint));
            return;
        }

        Vector3 insidePoint = hit.point - hit.normal * voxelGrid.VoxelSize * 0.02f;
        Vector3Int existingCell = voxelGrid.WorldToVoxel(insidePoint);

        if (!voxelModel.HasVoxel(existingCell)) { ClearTarget(); return; }

        Vector3 localNormal = voxelGrid.transform.InverseTransformDirection(hit.normal);
        Vector3Int direction = GetGridDirection(localNormal);

        SetPreviewTarget(existingCell + direction);
    }

    public void UpdateFreeTarget(Vector3Int cell)
    {
        if (!CanUseBrush()) return;
        SetPreviewTarget(cell);
    }

    private void SetPreviewTarget(Vector3Int cell)
    {
        if (!voxelGrid.IsInsideGrid(cell) || voxelModel.HasVoxel(cell)) { ClearTarget(); return; }

        bool targetChanged = !hasValidTarget || targetCell != cell;

        targetCell = cell;
        hasValidTarget = true;

        preview.transform.localPosition = voxelGrid.transform.InverseTransformPoint(voxelGrid.VoxelToWorld(cell));
        preview.transform.localRotation = Quaternion.identity;
        preview.transform.localScale = Vector3.one * voxelGrid.VoxelSize;

        if (!preview.activeSelf) preview.SetActive(true);
        if (targetChanged) { Debug.Log($"BRUSH PREVIEW: Target cell {cell}", this); }
    }

    private bool CanUseBrush()
    {
        if (!isInitialized || !isActiveAndEnabled) return false;
        if (toolManager.ActiveTool != HandTool.Brush) { ClearTarget(); return false; }
        return true;
    }

    private Vector3Int GetGridDirection(Vector3 normal)
    {
        Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

        if (abs.x >= abs.y && abs.x >= abs.z) return new Vector3Int(normal.x >= 0 ? 1 : -1, 0, 0);
        if (abs.y >= abs.x && abs.y >= abs.z) return new Vector3Int(0, normal.y >= 0 ? 1 : -1, 0);

        return new Vector3Int(0, 0, normal.z >= 0 ? 1 : -1);
    }

    public void PlaceVoxel()
    {
        if (!CanUseBrush()) return;
        if (!hasValidTarget) { Debug.Log("BrushTool: No valid target.", this); return; }
        if (!voxelGrid.IsInsideGrid(targetCell)) { Debug.LogWarning($"BrushTool: Outside grid {targetCell}", this); ClearTarget(); return; }
        if (voxelModel.HasVoxel(targetCell)) { Debug.LogWarning($"BrushTool: Cell occupied {targetCell}", this); ClearTarget(); return; }

        bool placed = voxelModel.AddVoxel(targetCell, brushColor);

        if (placed) { Debug.Log($"BRUSH: Placed voxel at {targetCell} | Color: {brushColor}", this); ClearTarget(); }
        else { Debug.LogWarning($"BrushTool: Placement failed at {targetCell}", this); }
    }

    public void PaintCell(Vector3Int cell)
    {
        if (!CanUseBrush()) return;
        if (!voxelGrid.IsInsideGrid(cell)) return;
        if (voxelModel.HasVoxel(cell)) return;

        UpdateFreeTarget(cell);
        PlaceVoxel();
    }

    public void ClearTarget()
    {
        hasValidTarget = false;
        if (preview != null) preview.SetActive(false);
    }

    private void OnDestroy() { if (preview != null) Destroy(preview); }
}