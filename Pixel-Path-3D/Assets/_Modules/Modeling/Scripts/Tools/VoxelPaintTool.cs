using System;
using UnityEngine;

public class VoxelPaintTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelModel voxelModel;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ColorPaletteManager colorManager;
    [SerializeField] private UndoRedoManager undoRedoManager;

    public bool PaintSelected()
    {
        if (voxelModel == null || selectionManager == null || colorManager == null) { Debug.LogWarning("[Paint] Missing references.", this); return false; }
        if (!selectionManager.HasSelection) { Debug.LogWarning("[Paint] No voxels selected.", this); return false; }
        int painted = 0;
        Color color = colorManager.CurrentColor;
        bool ownTransaction = undoRedoManager != null && !undoRedoManager.IsTransactionActive;
        if (ownTransaction) undoRedoManager.BeginTransaction();
        try
        {
            foreach (Vector3Int cell in selectionManager.Selected)
            {
                if (!voxelModel.TryGetVoxelColor(cell, out Color oldColor) || oldColor == color) continue;
                if (!voxelModel.SetVoxelColor(cell, color)) continue;
                painted++;
                if (undoRedoManager != null)
                {
                    Vector3Int savedCell = cell;
                    Color previousColor = oldColor;
                    Color newColor = color;
                    VoxelModel model = voxelModel;

                    undoRedoManager.RecordAction(
                        undo: () => { if (model == null || !model.HasVoxel(savedCell) || !model.SetVoxelColor(savedCell, previousColor)) throw new InvalidOperationException($"Cannot undo paint at {savedCell}."); },
                        redo: () => { if (model == null || !model.HasVoxel(savedCell) || !model.SetVoxelColor(savedCell, newColor)) throw new InvalidOperationException($"Cannot redo paint at {savedCell}."); }
                    );
                }
            }

            foreach (VoxelObject obj in selectionManager.SelectedObjects)
            {
                if (obj == null || obj.Model == null) continue;
                var cells = new System.Collections.Generic.List<Vector3Int>(obj.Model.Voxels.Keys);

                foreach (Vector3Int cell in cells)
                {
                    if (!obj.Model.TryGetVoxelColor(cell, out Color oldColor) || oldColor == color) continue;
                    if (!obj.Model.SetVoxelColor(cell, color)) continue;

                    painted++;

                    if (undoRedoManager != null)
                    {
                        Vector3Int savedCell = cell;
                        Color previousColor = oldColor;
                        Color newColor = color;
                        VoxelModel model = obj.Model;

                        undoRedoManager.RecordAction(
                            undo: () => { if (model == null || !model.HasVoxel(savedCell) || !model.SetVoxelColor(savedCell, previousColor)) throw new InvalidOperationException($"Cannot undo Free paint at {savedCell}."); },
                            redo: () => { if (model == null || !model.HasVoxel(savedCell) || !model.SetVoxelColor(savedCell, newColor)) throw new InvalidOperationException($"Cannot redo Free paint at {savedCell}."); }
                        );
                    }
                }
            }
        }
        finally { if (ownTransaction) undoRedoManager.EndTransaction(); }

        Debug.Log($"[Paint] Painted {painted} voxels.", this);
        return painted > 0;
    }

    public void PaintSelectedFromButton() { PaintSelected(); }
}