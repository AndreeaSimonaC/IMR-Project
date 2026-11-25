using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRScaleInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private XRTwoHandManipulation manipulation;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private ScaleTool scaleTool;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ModelingSettings modelingSettings;

    [Header("Scaling")]
    [SerializeField, Min(1.01f)] private float scaleThreshold = 1.5f;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;
    private bool scaledThisGesture;
    private bool freeScaling;
    private float appliedScaleFactor = 1f;

    private void Start()
    {
        if (coordinator == null || manipulation == null || toolManager == null || scaleTool == null)
        {
            Debug.LogError("XRScaleInput: Missing references!", this);
            enabled = false;
            return;
        }

        coordinator.RequireFreshPinch();
        Debug.Log("XRScaleInput: Initialized.", this);
    }

    private void Update()
    {
        bool available = toolManager.ActiveTool == HandTool.Scale && scaleTool.HasSelection && !interactionBlocked && !coordinator.IsMenuOpen && !IsMenuOpen();

        if (!available)
        {
            if (wasAvailable) coordinator.RequireFreshPinch();

            wasAvailable = false;
            ResetScaling();
            return;
        }

        wasAvailable = true;

        if (!manipulation.IsManipulating)
        {
            ResetScaling();
            return;
        }

        if (manipulation.StartedThisFrame)
        {
            ResetScaling();
            freeScaling = modelingSettings != null && modelingSettings.CurrentMode == PlacementMode.Free && selectionManager != null && selectionManager.ObjectSelectionCount > 0 && selectionManager.Count == 0;
        }

        if (freeScaling)
        {
            UpdateFreeScaling();
            return;
        }

        if (scaledThisGesture) return;
        if (manipulation.ScaleFactor < scaleThreshold) return;
        if (scaleTool.ScaleUniform2())
        {
            scaledThisGesture = true;
            Debug.Log("[XRScaleInput] Selection scaled 2x.", this);
        }
    }

    private bool IsMenuOpen()
    {
        if (expandedMenus == null) return false;
        foreach (GameObject menu in expandedMenus) { if (menu != null && menu.activeInHierarchy) return true; }

        return false;
    }

    public void SetInteractionBlocked(bool blocked)
    {
        if (interactionBlocked == blocked) return;

        interactionBlocked = blocked;
        coordinator?.RequireFreshPinch();
        ResetScaling();
    }

    private void OnDisable()
    {
        wasAvailable = false;
        ResetScaling();
        coordinator?.RequireFreshPinch();
    }
    private void UpdateFreeScaling()
    {
        float currentFactor = manipulation.ScaleFactor;

        if (currentFactor <= 0f || appliedScaleFactor <= 0f) { return; }
        float incrementalFactor = currentFactor / appliedScaleFactor;
        if (Mathf.Abs(incrementalFactor - 1f) < 0.0001f) return;
        Vector3 factors = Vector3.one * incrementalFactor;
        if (scaleTool.ScaleObjectsBy(factors)) appliedScaleFactor = currentFactor;
    }

    private void ResetScaling()
    {
        scaledThisGesture = false;
        freeScaling = false;
        appliedScaleFactor = 1f;
    }
}