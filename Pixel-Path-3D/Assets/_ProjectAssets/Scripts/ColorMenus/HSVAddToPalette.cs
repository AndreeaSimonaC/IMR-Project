using UnityEngine;

public class HSVAddToPalette : MonoBehaviour
{
    [Header("HSV Picker")]
    public HSVColorPicker colorPicker;

    [Header("Add Button")]
    public Collider addButtonCollider;

    [Header("Custom Palette Slots")]
    public ColorSwatchButton[] customSwatches = new ColorSwatchButton[8];

    [Header("Touch Settings")]
    public float touchDistance = 0.015f;
    public float releaseDistance = 0.025f;

    private bool isTouching;

    private void Update()
    {
        if (addButtonCollider == null || !addButtonCollider.gameObject.activeInHierarchy)
        {
            isTouching = false;
            return;
        }

        Vector3 closestPoint = addButtonCollider.ClosestPoint(transform.position);
        float distance = Vector3.Distance(transform.position, closestPoint);

        if (!isTouching && distance <= touchDistance) { isTouching = true; AddColor(); }
        if (isTouching && distance > releaseDistance) { isTouching = false; }
    }

    public void AddColor()
    {
        if (colorPicker == null || colorPicker.hueSlider == null || colorPicker.saturationSlider == null || colorPicker.valueSlider == null) return;
        Color selectedColor = Color.HSVToRGB(colorPicker.hueSlider.value, colorPicker.saturationSlider.value, colorPicker.valueSlider.value);

        foreach (ColorSwatchButton swatch in customSwatches)
        {
            if (swatch == null) continue;
            if (!swatch.gameObject.activeSelf)
            {
                swatch.SetSwatchColor(selectedColor);
                swatch.gameObject.SetActive(true);

                Debug.Log("[HSV] Custom color added!");
                return;
            }
        }

        Debug.Log("[HSV] All custom palette slots are full!");
    }
}