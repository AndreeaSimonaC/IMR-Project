using UnityEngine;

public class WorkingLayerVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private Material planeMaterial;

    private GameObject workingPlane;
    private int currentHeight = -1;

    private void Start()
    {
        if (voxelGrid == null || planeMaterial == null)
        {
            Debug.LogError("WorkingLayerVisual: Missing references!", this);
            enabled = false;
            return;
        }

        workingPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        workingPlane.name = "WorkingLayerPlane";
        workingPlane.transform.SetParent(voxelGrid.transform, false);
        workingPlane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        workingPlane.transform.localScale = new Vector3(ProjectSettings.Width * voxelGrid.VoxelSize, ProjectSettings.Length * voxelGrid.VoxelSize, 1f);
        workingPlane.GetComponent<Renderer>().sharedMaterial = planeMaterial;
        workingPlane.layer = 2;

        Collider col = workingPlane.GetComponent<Collider>();
        if (col != null) { col.enabled = false; Destroy(col); }

        workingPlane.SetActive(false);

        Debug.Log("WorkingLayerVisual: Initialized successfully.", this);
    }

    public void ShowLayer(int height)
    {
        if (workingPlane == null || voxelGrid == null) return;

        int clampedHeight = Mathf.Clamp(height, 0, ProjectSettings.Height - 1);
        float localY = clampedHeight * voxelGrid.VoxelSize + 0.001f;
        workingPlane.transform.localPosition = new Vector3(0f, localY, 0f);

        if (!workingPlane.activeSelf) workingPlane.SetActive(true);
        if (currentHeight != clampedHeight) { currentHeight = clampedHeight; Debug.Log($"WORKING LAYER: Showing Y = {currentHeight}", this); }
    }

    public void HideLayer() { if (workingPlane != null && workingPlane.activeSelf) { workingPlane.SetActive(false); Debug.Log("WORKING LAYER: Hidden.", this); } }

    private void OnDisable() { HideLayer(); }

    private void OnDestroy() { if (workingPlane != null) Destroy(workingPlane); }
}