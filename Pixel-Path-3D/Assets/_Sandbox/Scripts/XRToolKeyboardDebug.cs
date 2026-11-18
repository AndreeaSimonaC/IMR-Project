#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;

public class XRToolKeyboardDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private BrushTool brushTool;
    [SerializeField] private EraserTool eraserTool;
    [SerializeField] private SelectTool selectTool;
    [SerializeField] private MoveTool moveTool;
    [SerializeField] private RotateTool rotateTool;
    [SerializeField] private ScaleTool scaleTool;

    [Header("Debug")]
    [SerializeField] private bool logActions = true;

    private void Update()
    {
        if (toolManager == null || Keyboard.current == null) return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard.digit1Key.wasPressedThisFrame) ActivateTool(HandTool.Brush);
        else if (keyboard.digit2Key.wasPressedThisFrame) ActivateTool(HandTool.Eraser);
        else if (keyboard.digit3Key.wasPressedThisFrame) ActivateTool(HandTool.Select);
        else if (keyboard.digit4Key.wasPressedThisFrame) ActivateTool(HandTool.Move);
        else if (keyboard.digit5Key.wasPressedThisFrame) ActivateTool(HandTool.Rotate);
        else if (keyboard.digit6Key.wasPressedThisFrame) ActivateTool(HandTool.Scale);

        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (!shift) return;

        switch (toolManager.ActiveTool)
        {
            case HandTool.Brush:
                if (keyboard.bKey.wasPressedThisFrame && brushTool != null)
                {
                    brushTool.PlaceVoxel();
                    LogExecuted("Brush");
                }
                break;

            case HandTool.Eraser:
                if (keyboard.eKey.wasPressedThisFrame)
                    LogAction("Eraser", eraserTool != null && eraserTool.EraseTarget());
                break;

            case HandTool.Select:
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    bool hasTarget = selectTool != null && selectTool.HasTarget;
                    if (hasTarget) selectTool.SelectTarget(true);
                    LogAction("Select", hasTarget);
                }
                break;

            case HandTool.Move:
                if (moveTool == null) break;

                if (keyboard.iKey.wasPressedThisFrame) LogAction("Move Forward", moveTool.MoveForward());
                if (keyboard.kKey.wasPressedThisFrame) LogAction("Move Backward", moveTool.MoveBackward());
                if (keyboard.jKey.wasPressedThisFrame) LogAction("Move Left", moveTool.MoveLeft());
                if (keyboard.lKey.wasPressedThisFrame) LogAction("Move Right", moveTool.MoveRight());
                if (keyboard.uKey.wasPressedThisFrame) LogAction("Move Up", moveTool.MoveUp());
                if (keyboard.oKey.wasPressedThisFrame) LogAction("Move Down", moveTool.MoveDown());
                break;

            case HandTool.Rotate:
                if (rotateTool == null) break;
                if (keyboard.rKey.wasPressedThisFrame) LogAction("Rotate X", rotateTool.RotateX());
                if (keyboard.tKey.wasPressedThisFrame) LogAction("Rotate Y", rotateTool.RotateY());
                if (keyboard.pKey.wasPressedThisFrame) LogAction("Rotate Z", rotateTool.RotateZ());

                break;

            case HandTool.Scale:
                if (keyboard.gKey.wasPressedThisFrame)
                    LogAction("Scale 2x", scaleTool != null && scaleTool.ScaleUniform2());
                break;
        }
    }

    private void ActivateTool(HandTool tool)
    {
        toolManager.SelectTool(tool);
        if (logActions) Debug.Log($"[XR Keyboard Debug] Activated: {tool}", this);
    }

    private void LogAction(string action, bool success)
    {
        if (logActions) Debug.Log($"[XR Keyboard Debug] {action}: {(success ? "SUCCESS" : "FAILED")}", this);
    }

    private void LogExecuted(string action)
    {
        if (logActions) Debug.Log($"[XR Keyboard Debug] {action}: Executed", this);
    }
}

#endif