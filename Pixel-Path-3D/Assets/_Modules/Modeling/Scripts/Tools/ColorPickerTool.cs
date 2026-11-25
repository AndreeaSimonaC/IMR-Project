using UnityEngine;

public class ColorPickerTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private ColorPaletteManager colorManager;
    [SerializeField] private VoxelRenderer voxelRenderer;

    private Vector3Int targetCell;
    private VoxelObject targetObject;
    private bool hasTarget;
    public bool HasTarget => hasTarget;
    public Vector3Int TargetCell => targetCell;
    public VoxelObject TargetObject => targetObject;

    private bool CanUsePicker() { return isActiveAndEnabled && toolManager != null && toolManager.ActiveTool == HandTool.ColorPicker && voxelGrid != null && voxelModel != null && colorManager != null; }

    public void UpdateCellTarget(Vector3Int cell)
    {
        ClearTarget();

        if (!CanUsePicker()) return;
        if (!voxelGrid.IsInsideGrid(cell)) return;
        if (!voxelModel.HasVoxel(cell)) return;

        targetCell = cell;
        hasTarget = true;
    }

    public bool PickColor()
    {
        if (!CanUsePicker() || !hasTarget) return false;
        VoxelModel targetModel = targetObject != null ? targetObject.Model : voxelModel;
        if (targetModel == null || !targetModel.TryGetVoxelColor( targetCell, out Color pickedColor))
        {
            ClearTarget();
            return false;
        }
        colorManager.SetColor(pickedColor);
        Debug.Log($"[ColorPicker] Picked " + $"#{ColorUtility.ToHtmlStringRGB(pickedColor)} " + $"from voxel {targetCell}", this);

        return true;
    }

    public void ClearTarget() { hasTarget = false; targetObject = null; }

    private void OnDisable() { ClearTarget(); }

    public void UpdateTarget(RaycastHit hit)
    {
        ClearTarget();

        if (!CanUsePicker() || hit.collider == null) return;

        if (voxelRenderer != null && voxelRenderer.TryGetObject(hit.collider.transform, out VoxelObject obj) && obj != null && obj.Model != null && obj.Model != voxelModel)
        {
            if (obj.Model.VoxelCount != 1) return;
            foreach (Vector3Int cell in obj.Model.Voxels.Keys) { UpdateObjectTarget(obj, cell); return; }

            return;
        }

        Vector3 inward = hit.point - hit.normal * voxelGrid.VoxelSize * 0.02f;
        UpdateCellTarget(voxelGrid.WorldToVoxel(inward));
    }

    public void UpdateObjectTarget(VoxelObject obj, Vector3Int localCell)
    {
        ClearTarget();

        if (!CanUsePicker() || obj == null || obj.Model == null) return;
        if (!obj.Model.HasVoxel(localCell)) return;

        targetObject = obj;
        targetCell = localCell;
        hasTarget = true;
    }
}