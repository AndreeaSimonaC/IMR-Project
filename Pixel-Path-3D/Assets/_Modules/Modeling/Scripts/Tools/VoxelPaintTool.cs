using UnityEngine;

public class VoxelPaintTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ColorPaletteManager colorManager;

    public bool PaintSelected()
    {
        if (voxelModel == null || selectionManager == null || colorManager == null) { Debug.LogWarning("[Paint] Missing references.", this); return false; }
        if (!selectionManager.HasSelection) { Debug.LogWarning("[Paint] No voxels selected.", this); return false; }
        int painted = 0;

        foreach (Vector3Int cell in selectionManager.Selected) { if (voxelModel.SetVoxelColor(cell, colorManager.CurrentColor)) painted++; }

        Debug.Log($"[Paint] Painted {painted} voxels.", this);
        return painted > 0;
    }

    public void PaintSelectedFromButton() { PaintSelected(); }
}