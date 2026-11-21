using UnityEngine;
using TMPro;

public class HSVColorPicker : MonoBehaviour
{
    [Header("HSV Sliders")]
    public HSVSliderHandle hueSlider;
    public HSVSliderHandle saturationSlider;
    public HSVSliderHandle valueSlider;

    [Header("Color Display")]
    public Renderer colorPreview;
    public TMP_Text hexText;

    [Header("Gradient Tracks")]
    public Renderer saturationTrack;
    public Renderer valueTrack;
    [Header("Color Manager")]
    public ColorPaletteManager colorManager;

    [Header("Shader Graph Properties")]
    public string saturationColorProperty = "_HueColor";
    public string valueColorProperty = "_FullColor";

    private MaterialPropertyBlock propertyBlock;

    private void OnEnable()
    {
        if (hueSlider != null) hueSlider.OnValueChanged += HandleValueChanged;
        if (saturationSlider != null) saturationSlider.OnValueChanged += HandleValueChanged;
        if (valueSlider != null) valueSlider.OnValueChanged += HandleValueChanged;
        if (colorManager != null) colorManager.OnColorChanged += SyncFromColor;
        SyncFromCurrentColor();
    }

    private void Start()
    {
        if (hueSlider != null) hueSlider.SetValue(hueSlider.value);
        if (saturationSlider != null) saturationSlider.SetValue(saturationSlider.value);
        if (valueSlider != null) valueSlider.SetValue(valueSlider.value);

        SyncFromCurrentColor();
    }

    private void OnDisable()
    {
        if (hueSlider != null) hueSlider.OnValueChanged -= HandleValueChanged;
        if (saturationSlider != null) saturationSlider.OnValueChanged -= HandleValueChanged;
        if (valueSlider != null) valueSlider.OnValueChanged -= HandleValueChanged;
        if (colorManager != null) colorManager.OnColorChanged -= SyncFromColor;
    }

    private void HandleValueChanged(float newValue) { UpdateColor(); }
    private void SyncFromCurrentColor()
    {
        if (colorManager != null) SyncFromColor(colorManager.CurrentColor);
        else UpdateColor();
    }
    private void SyncFromColor(Color color)
    {
        if (hueSlider == null || saturationSlider == null || valueSlider == null) return;
        Color.RGBToHSV(color, out float h, out float s, out float v);
        hueSlider.SetValue(h);
        saturationSlider.SetValue(s);
        valueSlider.SetValue(v);
        UpdateColor();
        Debug.Log($"[HSV] Synced from color #{ColorUtility.ToHtmlStringRGB(color)}", this);
    }

    [ContextMenu("Test - Update HSV Color")]
    public void UpdateColor()
    {
        if (hueSlider == null || saturationSlider == null || valueSlider == null) return;

        float h = hueSlider.value;
        float s = saturationSlider.value;
        float v = valueSlider.value;

        Color selectedColor = Color.HSVToRGB(h, s, v);
        Color pureHue = Color.HSVToRGB(h, 1f, 1f);
        Color fullColor = Color.HSVToRGB(h, s, 1f);

        SetRendererColor(colorPreview, "_BaseColor", selectedColor);
        SetRendererColor(saturationTrack, saturationColorProperty, pureHue);
        SetRendererColor(valueTrack, valueColorProperty, fullColor);
        if (hexText != null) { hexText.text = "#" + ColorUtility.ToHtmlStringRGB(selectedColor); }
    }

    private void SetRendererColor(Renderer target, string propertyName, Color color)
    {
        if (target == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(propertyName, color);
        target.SetPropertyBlock(propertyBlock);
    }

    [ContextMenu("Test - Sync HSV Sliders")]
    private void TestSyncSliders()
    {
        if (hueSlider == null || saturationSlider == null || valueSlider == null) return;

        hueSlider.SetValue(0.5f);
        saturationSlider.SetValue(0.7f);
        valueSlider.SetValue(0.9f);

        UpdateColor();
    }
}