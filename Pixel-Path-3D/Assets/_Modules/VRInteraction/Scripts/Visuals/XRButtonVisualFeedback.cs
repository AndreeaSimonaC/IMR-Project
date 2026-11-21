using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class XRButtonVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer outlineRenderer;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.magenta;

    [Header("Active State")]
    [SerializeField] private PalmMenuTab menuTab = PalmMenuTab.None;
    [SerializeField] private HandMenuController menuController;
    [SerializeField] private bool isToolButton = false;
    [SerializeField] private HandTool tool;
    [SerializeField] private ToolManager toolManager;

    private XRSimpleInteractable interactable;
    private MaterialPropertyBlock block;
    private bool pressed;
    private bool active;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        block = new MaterialPropertyBlock();
        SetOutlineColor(normalColor);
    }

    private void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);

        if (menuTab != PalmMenuTab.None)
        {
            if (menuController == null) menuController = FindFirstObjectByType<HandMenuController>();
            if (menuController != null)
            {
                menuController.OnTabChanged += OnTabChanged;
                OnTabChanged(menuController.ActiveTab);
            }
        }
        if (isToolButton)
        {
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
                toolManager.OnToolChanged += OnToolChanged;
                OnToolChanged(toolManager.ActiveTool);
            }
        }

        RefreshVisual();
    }

    private void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);

        if (menuController != null) menuController.OnTabChanged -= OnTabChanged;
        if (toolManager != null) toolManager.OnToolChanged -= OnToolChanged;

        pressed = false;
    }

    private void OnHoverEntered(HoverEnterEventArgs args) { RefreshVisual(); }

    private void OnHoverExited(HoverExitEventArgs args) { RefreshVisual(); }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        pressed = true;
        RefreshVisual();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        pressed = false;
        RefreshVisual();
    }

    private void OnTabChanged(PalmMenuTab tab)
    {
        active = tab == menuTab;
        RefreshVisual();
    }

    private void OnToolChanged(HandTool newTool)
    {
        active = newTool == tool;
        Debug.Log($"[VISUAL] {gameObject.name} | Tool={newTool} | Active={active}", this);

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (menuTab != PalmMenuTab.None || isToolButton)
        {
            if (active) SetOutlineColor(pressedColor);
            else if (interactable != null && interactable.isHovered) SetOutlineColor(hoverColor);
            else SetOutlineColor(normalColor);

            return;
        }

        if (pressed) SetOutlineColor(pressedColor);
        else if (interactable != null && interactable.isHovered) SetOutlineColor(hoverColor);
        else SetOutlineColor(normalColor);
    }

    private void SetOutlineColor(Color color)
    {
        if (outlineRenderer == null || block == null) return;

        outlineRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", color * 2f);
        outlineRenderer.SetPropertyBlock(block);
    }
}