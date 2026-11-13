#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;

public class EditorToolTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;

    private void Start()
    {
        Debug.Log("Editor tool tester running.", this);
        if (toolManager == null) { Debug.LogError("EditorToolTester: ToolManager ref missing!", this); return; }
        Debug.Log("EditorToolTester started. Keyboard testing active.", this);
    }

    private void Update()
    {
        if (toolManager == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.anyKey.wasPressedThisFrame) { Debug.Log("Keyboard input detected.", this); }

        if (keyboard.digit1Key.wasPressedThisFrame) Select(HandTool.Brush);
        if (keyboard.digit2Key.wasPressedThisFrame) Select(HandTool.Eraser);
        if (keyboard.digit3Key.wasPressedThisFrame) Select(HandTool.Select);
        if (keyboard.digit4Key.wasPressedThisFrame) Select(HandTool.Move);
        if (keyboard.digit5Key.wasPressedThisFrame) Select(HandTool.Rotate);
        if (keyboard.digit6Key.wasPressedThisFrame) Select(HandTool.Scale);
        if (keyboard.digit7Key.wasPressedThisFrame) Select(HandTool.Duplicate);
        if (keyboard.digit8Key.wasPressedThisFrame) Select(HandTool.Delete);
        if (keyboard.digit9Key.wasPressedThisFrame) Select(HandTool.Group);
        if (keyboard.zKey.wasPressedThisFrame) Select(HandTool.Ungroup);
        if (keyboard.xKey.wasPressedThisFrame) Select(HandTool.Paint);
        if (keyboard.cKey.wasPressedThisFrame) Select(HandTool.ColorPicker);
        if (keyboard.vKey.wasPressedThisFrame) Select(HandTool.ColorPalette);
    }
    private void Select(HandTool tool) { Debug.Log($"Editor test: Chosen {tool}", this); toolManager.SelectTool(tool); }
}

#endif