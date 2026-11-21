using UnityEngine;

public class ColorPickerTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private ColorPaletteManager colorManager;

    private Vector3Int targetCell;
    private bool hasTarget;
    public bool HasTarget => hasTarget;
    public Vector3Int TargetCell => targetCell;

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
        if (!voxelModel.TryGetVoxelColor( targetCell, out Color pickedColor))
        {
            ClearTarget();
            return false;
        }
        colorManager.SetColor(pickedColor);
        Debug.Log($"[ColorPicker] Picked " + $"#{ColorUtility.ToHtmlStringRGB(pickedColor)} " + $"from voxel {targetCell}", this);

        return true;
    }

    public void ClearTarget() { hasTarget = false; }

    private void OnDisable() { ClearTarget(); }
}