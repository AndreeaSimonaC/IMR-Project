using UnityEngine;

[DefaultExecutionOrder(-200)]
public class XRScaleInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;
    [SerializeField] private XRTwoHandManipulation manipulation;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private ScaleTool scaleTool;

    [Header("Scaling")]
    [SerializeField, Min(1.01f)] private float scaleThreshold = 1.5f;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;

    private bool interactionBlocked;
    private bool wasAvailable;
    private bool scaledThisGesture;

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
            scaledThisGesture = false;
            return;
        }

        wasAvailable = true;

        if (!manipulation.IsManipulating)
        {
            scaledThisGesture = false;
            return;
        }

        if (manipulation.StartedThisFrame)
            scaledThisGesture = false;

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

        foreach (GameObject menu in expandedMenus)
        {
            if (menu != null && menu.activeInHierarchy) return true;
        }

        return false;
    }

    public void SetInteractionBlocked(bool blocked)
    {
        if (interactionBlocked == blocked) return;

        interactionBlocked = blocked;
        coordinator?.RequireFreshPinch();
        scaledThisGesture = false;
    }

    private void OnDisable()
    {
        wasAvailable = false;
        scaledThisGesture = false;
        coordinator?.RequireFreshPinch();
    }
}