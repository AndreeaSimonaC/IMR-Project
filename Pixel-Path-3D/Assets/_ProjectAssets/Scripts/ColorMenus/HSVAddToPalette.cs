using UnityEngine;

public class HSVAddToPalette : MonoBehaviour
{
    [Header("HSV Picker")]
    public HSVColorPicker colorPicker;

    [Header("Add Button")]
    public Collider addButtonCollider;

    [Header("Custom Palette Pagination")]
    public CustomPalettePagination pagination;

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

        if (pagination == null)
        {
            Debug.LogWarning("[HSV] Custom Palette Pagination missing!");
            return;
        }

        pagination.AddColor(selectedColor);
        Debug.Log("[HSV] Custom color added!");
    }
}