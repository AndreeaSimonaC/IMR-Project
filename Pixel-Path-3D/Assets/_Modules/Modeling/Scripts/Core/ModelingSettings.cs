using System;
using UnityEngine;

public enum PlacementMode { SnapToGrid, Free }

public class ModelingSettings : MonoBehaviour
{
    [SerializeField] private PlacementMode placementMode = PlacementMode.SnapToGrid;
    public PlacementMode CurrentMode => placementMode;
    public event Action<PlacementMode> OnPlacementModeChanged;
    public void SetSnapToGrid() { SetMode(PlacementMode.SnapToGrid); }
    public void SetFreePlacement() { SetMode(PlacementMode.Free); }

    private void SetMode(PlacementMode newMode)
    {
        if (placementMode == newMode) return;
        placementMode = newMode;
        OnPlacementModeChanged?.Invoke(placementMode);
        Debug.Log($"[ModelingSettings] Placement mode: {placementMode}", this);
    }
}