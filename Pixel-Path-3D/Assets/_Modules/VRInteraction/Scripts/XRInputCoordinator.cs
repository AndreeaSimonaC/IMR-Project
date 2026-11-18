using UnityEngine;

[DefaultExecutionOrder(-400)]
public class XRInputCoordinator : MonoBehaviour
{
    public enum ToolType { None, Brush, Select, Eraser, Move, Rotate, Scale }

    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private XRHandInput rightHand;
    [SerializeField] private XRHandInput leftHand;

    [Header("Menu Blocking")]
    [SerializeField] private GameObject[] expandedMenus;
    [SerializeField] private bool menuOpen;

    private ToolType activeTool = ToolType.None;
    private bool previousMenuOpen;

    public XRHandInput RightHand => rightHand;
    public XRHandInput LeftHand => leftHand;
    public ToolType ActiveTool => activeTool;
    public bool IsMenuOpen => menuOpen || AreMenusExpanded();
    public bool CanInteract => !IsMenuOpen && activeTool != ToolType.None;
    public bool BothHandsTracked => rightHand != null && leftHand != null && rightHand.IsTracked && leftHand.IsTracked;
    public bool BothHandsPinching => CanInteract && BothHandsTracked && rightHand.IsPinching && leftHand.IsPinching;

    private void OnEnable()
    {
        if (toolManager != null)
        {
            toolManager.OnToolChanged += OnToolChanged;
            OnToolChanged(toolManager.ActiveTool);
        }

        previousMenuOpen = IsMenuOpen;
        RequireFreshPinch();
    }

    private void OnDisable()
    {
        if (toolManager != null) toolManager.OnToolChanged -= OnToolChanged;

        RequireFreshPinch();
    }

    private void Update()
    {
        bool currentMenuOpen = IsMenuOpen;

        if (currentMenuOpen != previousMenuOpen)
        {
            previousMenuOpen = currentMenuOpen;
            RequireFreshPinch();
        }
    }

    private void OnToolChanged(HandTool tool)
    {
        ToolType newTool = tool switch
        {
            HandTool.Brush => ToolType.Brush,
            HandTool.Select => ToolType.Select,
            HandTool.Eraser => ToolType.Eraser,
            HandTool.Move => ToolType.Move,
            HandTool.Rotate => ToolType.Rotate,
            HandTool.Scale => ToolType.Scale,
            _ => ToolType.None
        };

        SetActiveTool(newTool);
    }

    public bool IsToolActive(ToolType tool)
    {
        return CanInteract && activeTool == tool;
    }

    public void SetActiveTool(ToolType tool)
    {
        if (activeTool == tool) return;

        activeTool = tool;
        RequireFreshPinch();
    }

    public void SetMenuOpen(bool isOpen)
    {
        if (menuOpen == isOpen) return;

        menuOpen = isOpen;
        previousMenuOpen = IsMenuOpen;
        RequireFreshPinch();
    }

    private bool AreMenusExpanded()
    {
        if (expandedMenus == null) return false;

        foreach (GameObject menu in expandedMenus)
        {
            if (menu != null && menu.activeInHierarchy) return true;
        }

        return false;
    }

    public void RequireFreshPinch()
    {
        if (rightHand != null) rightHand.RequireFreshPinch();
        if (leftHand != null) leftHand.RequireFreshPinch();
    }

    public bool CanUseRightHand(ToolType tool)
    {
        return IsToolActive(tool) && rightHand != null && rightHand.IsTracked;
    }

    public bool CanUseLeftHand(ToolType tool)
    {
        return IsToolActive(tool) && leftHand != null && leftHand.IsTracked;
    }

    public bool CanUseBothHands(ToolType tool)
    {
        return IsToolActive(tool) && BothHandsTracked;
    }
}