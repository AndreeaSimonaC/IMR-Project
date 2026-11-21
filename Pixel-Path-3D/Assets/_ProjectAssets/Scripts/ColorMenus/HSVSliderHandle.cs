using System;
using UnityEngine;

public class HSVSliderHandle : MonoBehaviour
{
    [Header("Slider Limits")]
    public Transform startPoint;
    public Transform endPoint;

    [Range(0f, 1f)]
    public float value = 0f;

    public event Action<float> OnValueChanged;

    public void SetValue(float newValue)
    {
        value = Mathf.Clamp01(newValue);

        if (startPoint != null && endPoint != null)
        {
            transform.position = Vector3.Lerp(startPoint.position, endPoint.position, value);
        }

        OnValueChanged?.Invoke(value);
    }

    [ContextMenu("Test - Move to Middle")]
    private void TestMiddle()
    {
        SetValue(0.5f);
    }
}