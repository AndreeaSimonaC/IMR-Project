using System;
using System.Collections.Generic;
using UnityEngine;

public class DuplicateTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private VoxelEditOperations editOperations;
    [SerializeField] private VoxelObjectManager objectManager;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private ModelingSettings modelingSettings;
    [SerializeField] private UndoRedoManager undoRedoManager;

    public bool HasSelection => selectionManager != null && selectionManager.HasSelection;

    private bool CanUseDuplicate() { return isActiveAndEnabled && toolManager != null && toolManager.ActiveTool == HandTool.Duplicate && selectionManager != null && selectionManager.HasSelection && editOperations != null; }

    public bool DuplicateBy(Vector3Int delta)
    {
        if (!CanUseDuplicate() || delta == Vector3Int.zero) return false;

        if (selectionManager.ObjectSelectionCount > 0)
        {
            if (voxelGrid == null || selectionManager.Count > 0) return false;
            Vector3 worldDelta = voxelGrid.transform.TransformVector((Vector3)delta * voxelGrid.VoxelSize);
            return DuplicateObjectsBy(worldDelta);
        }

        int originalCount = selectionManager.Count;
        bool duplicated = editOperations.DuplicateSelected(delta);

        if (duplicated) { Debug.Log($"[DuplicateTool] Duplicated {originalCount} voxels by {delta}.", this); }

        return duplicated;
    }

    public bool DuplicateObjectsBy(Vector3 worldDelta)
    {
        if (!CanUseDuplicate() || objectManager == null || selectionManager.ObjectSelectionCount == 0 || selectionManager.Count > 0) return false;
        if (modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.SnapToGrid)
        {
            if (voxelGrid == null || voxelGrid.VoxelSize <= 0f) return false;
            float size = voxelGrid.VoxelSize;
            Vector3 localDelta = voxelGrid.transform.InverseTransformVector(worldDelta);
            localDelta = new Vector3(Mathf.Round(localDelta.x / size) * size, Mathf.Round(localDelta.y / size) * size, Mathf.Round(localDelta.z / size) * size);
            worldDelta = voxelGrid.transform.TransformVector(localDelta);
        }

        if (worldDelta.sqrMagnitude < 0.00000001f) return false;

        var sources = new List<VoxelObject>(selectionManager.SelectedObjects);
        var duplicates = new List<VoxelObject>();

        foreach (VoxelObject source in sources)
        {
            if (source == null || source.Model == null) continue;
            VoxelObject copy = objectManager.CreateObject(source.name + "_Copy", source.transform.position + worldDelta);
            if (copy == null || copy.Model == null) continue;
            copy.transform.rotation = source.transform.rotation;
            copy.transform.localScale = source.transform.localScale;
            foreach (var voxel in source.Model.Voxels) { copy.Model.AddVoxel(voxel.Key, voxel.Value); }
            duplicates.Add(copy);
        }

        if (duplicates.Count == 0) return false;
        selectionManager.ReplaceObjectSelection(duplicates);

        if (undoRedoManager != null)
        {
            VoxelObjectManager manager = objectManager;
            SelectionManager selection = selectionManager;
            var savedSources = new List<VoxelObject>(sources);
            var savedDuplicates = new List<VoxelObject>(duplicates);

            undoRedoManager.RecordAction(
                undo: () =>
                {
                    foreach (VoxelObject obj in savedDuplicates)
                    {
                        if (obj == null || !manager.UnregisterObject(obj)) throw new InvalidOperationException("Cannot undo Free duplication.");
                        obj.gameObject.SetActive(false);
                    }

                    if (selection != null) selection.ReplaceObjectSelection(savedSources);
                },
                redo: () =>
                {
                    foreach (VoxelObject obj in savedDuplicates)
                    {
                        if (obj == null) throw new InvalidOperationException("Cannot redo Free duplication.");
                        obj.gameObject.SetActive(true);
                        if (!manager.RegisterObject(obj))
                        {
                            obj.gameObject.SetActive(false);
                            throw new InvalidOperationException("Cannot restore duplicated object.");
                        }
                    }
                    if (selection != null) selection.ReplaceObjectSelection(savedDuplicates);
                }
            );
        }
        Debug.Log($"[DuplicateTool] Duplicated {duplicates.Count} object(s).", this);
        return true;
    }
}