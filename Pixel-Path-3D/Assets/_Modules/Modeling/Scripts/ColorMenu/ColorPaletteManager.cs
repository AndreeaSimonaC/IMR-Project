using System;
using UnityEngine;

public class ColorPaletteManager : MonoBehaviour
{
    [SerializeField] private Color initialColor = Color.white;

    public Color CurrentColor { get; private set; }
    public event Action<Color> OnColorChanged;

    private void Awake() => CurrentColor = initialColor;

    public void SetColor(Color color)
    {
        CurrentColor = color;
        OnColorChanged?.Invoke(color);
        Debug.Log($"[ColorPalette] Selected: {color}");
    }
}