#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;

public class EditorMouseInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera testCamera;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private BrushTool brushTool;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private WorkingLayerVisual workingLayerVisual;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField, Min(0.1f)] private float maxDistance = 100f;

    [Header("Free Placement")]
    [SerializeField] private bool freePlacementMode = false;
    [SerializeField, Min(0)] private int freeHeight = 0;

    private void Start()
    {
        if (testCamera == null) testCamera = Camera.main;
        if (testCamera == null || toolManager == null || brushTool == null || voxelGrid == null || workingLayerVisual == null)
        {
            Debug.LogError("EditorMouseInput: Missing references", this);
            enabled = false;
            return;
        }

        freeHeight = Mathf.Clamp(freeHeight, 0, ProjectSettings.Height - 1);
        Debug.Log("EditorMouseInput: Initialized successfully.", this);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null) return;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            freePlacementMode = !freePlacementMode;
            brushTool.ClearTarget();
            Debug.Log($"EDITOR MOUSE: Free Placement {(freePlacementMode ? "ON" : "OFF")}", this);
        }

        if (toolManager.ActiveTool != HandTool.Brush)
        {
            brushTool.ClearTarget();
            workingLayerVisual.HideLayer();
            return;
        }

        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = testCamera.ScreenPointToRay(mousePosition);

        if (freePlacementMode)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                int direction = scroll > 0f ? 1 : -1;
                int previousHeight = freeHeight;
                freeHeight = Mathf.Clamp(freeHeight + direction, 0, ProjectSettings.Height - 1);
                if (freeHeight != previousHeight) Debug.Log($"FREE PLACEMENT: Height Y = {freeHeight}", this);
            }
            workingLayerVisual.ShowLayer(freeHeight);

            float planeHeight = (freeHeight + 0.5f) * voxelGrid.VoxelSize;
            Vector3 planeCenter = voxelGrid.transform.TransformPoint(new Vector3(0f, planeHeight, 0f));

            Plane gridPlane = new Plane(voxelGrid.transform.up, planeCenter);
            bool hasPlaneHit = gridPlane.Raycast(ray, out float distance) && distance >= 0f && distance <= maxDistance;

            if (hasPlaneHit)
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                Vector3Int cell = voxelGrid.WorldToVoxel(hitPoint);
                cell.y = freeHeight;
                brushTool.UpdateFreeTarget(cell);
            }
            else { workingLayerVisual.HideLayer(); brushTool.ClearTarget(); }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Debug.Log($"EDITOR MOUSE: Free placement requested | Y = {freeHeight}", this);
                brushTool.PlaceVoxel();
            }
        }
        else
        {
            workingLayerVisual.HideLayer();
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayers, QueryTriggerInteraction.Ignore);
            if (hasHit) { brushTool.UpdateTarget(hit); }
            else { brushTool.ClearTarget(); }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (hasHit) { Debug.Log($"EDITOR MOUSE: Hit {hit.collider.name} at {hit.point} | Normal: {hit.normal}", this); }
                else { Debug.Log("EDITOR MOUSE: No collider detected.", this); }
                brushTool.PlaceVoxel();
            }
        }
    }

    private void OnDisable()
    {
        if (brushTool != null) brushTool.ClearTarget();
        if (workingLayerVisual != null) workingLayerVisual.HideLayer();
    }
}

#endif