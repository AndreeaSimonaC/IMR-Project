
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class NumberStepper : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private int value = 32;
    [SerializeField] private int min = 1;
    [SerializeField] private int max = 128;
    [SerializeField] private int step = 1;

    public UnityEvent<int> onValueChanged;

    private void Start()
    {
        value = Mathf.Clamp(value, min, max);
        UpdateDisplay();
        Debug.Log($"{gameObject.name}: NumberStepper initialized with value {value}");
    }

    public void Increase()
    {
        Debug.Log($"{gameObject.name}: Plus button pressed");
        SetValue(value + step);
    }

    public void Decrease()
    {
        Debug.Log($"{gameObject.name}: Minus button pressed");
        SetValue(value - step);
    }

    private void SetValue(int newValue)
    {
        int clampedValue = Mathf.Clamp(newValue, min, max);
        if (clampedValue == value) return;
        value = clampedValue;
        UpdateDisplay();
        Debug.Log($"{gameObject.name}: Value changed to {value}");
        onValueChanged?.Invoke(value);
    }

    private void UpdateDisplay()
    {
        if (valueText == null)
        {
            Debug.LogWarning($"{gameObject.name}: Value Text is not assigned!", this);
            return;
        }
        valueText.text = value.ToString();
    }

    public int GetValue()
    {
        return value;
    }
}
