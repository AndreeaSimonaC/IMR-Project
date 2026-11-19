using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class ColorSwatchButton : MonoBehaviour
{
    [SerializeField] private ColorPaletteManager colorManager;
    [SerializeField] private Color swatchColor = Color.white;
    [SerializeField] private Renderer colorRenderer;

    private XRSimpleInteractable interactable;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        propertyBlock = new MaterialPropertyBlock();
        UpdateVisual();
    }

    private void OnEnable() { interactable.selectEntered.AddListener(OnSelected); }
    private void OnDisable() { interactable.selectEntered.RemoveListener(OnSelected); }
    private void OnSelected(SelectEnterEventArgs args) => SelectColor();

    public void SelectColor()
    {
        if (colorManager == null) colorManager = FindFirstObjectByType<ColorPaletteManager>();
        if (colorManager == null) { Debug.LogWarning("[ColorSwatch] Color manager missing!", this); return; }
        colorManager.SetColor(swatchColor);
    }

    private void UpdateVisual()
    {
        if (colorRenderer == null) return;
        colorRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", swatchColor);
        propertyBlock.SetColor("_Color", swatchColor);
        colorRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        if (colorRenderer == null) return;
        var block = new MaterialPropertyBlock();
        colorRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", swatchColor);
        block.SetColor("_Color", swatchColor);
        colorRenderer.SetPropertyBlock(block);
    }
}