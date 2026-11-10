
using UnityEngine;
using UnityEngine.Events;

public enum HandTool
{ Brush, Eraser, Select, Move, Rotate, Scale, Duplicate, Delete, Group, Ungroup, Paint, ColorPicker, ColorPalette }

public class HandToolButton : MonoBehaviour
{
    [Header("Tool Settings")]
    [SerializeField] private HandTool tool;

    [Header("Events")]
    [SerializeField] private UnityEvent onToolPressed;

    private void Start()
    {
        Debug.Log($"HandToolButton initialized: {gameObject.name} - {tool}", this);
    }

    [ContextMenu("TEST - Press Tool")]
    public void Press()
    {
        Debug.Log($"TOOL PRESSED: {tool} | Button: {gameObject.name}", this);
        onToolPressed?.Invoke();
    }
}
