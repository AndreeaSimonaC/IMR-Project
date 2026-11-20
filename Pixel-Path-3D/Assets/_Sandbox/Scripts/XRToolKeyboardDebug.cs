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
    [SerializeField] private ColorPaletteManager colorManager;
    [SerializeField] private VoxelPaintTool paintTool;
    [SerializeField] private HandMenuController handMenuController;
    [SerializeField] private HandToolButton colorPickerButton;
    [SerializeField] private HandToolButton duplicateButton;
    [SerializeField] private HandToolButton deleteButton;
    [SerializeField] private HandToolButton groupButton;
    [SerializeField] private HandToolButton ungroupButton;
    [SerializeField] private HSVAddToPalette hsvAddToPalette;
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
        if (colorManager != null)
        {
            if (keyboard.digit7Key.wasPressedThisFrame)
            {
                Debug.Log($"[COLOR DEBUG] 7 pressed | Manager={colorManager != null}");
                if (colorManager != null) colorManager.SetColor(Color.red);
            }
            if (keyboard.digit8Key.wasPressedThisFrame) colorManager.SetColor(new Color32(77, 204, 128, 255));
            if (keyboard.digit9Key.wasPressedThisFrame) colorManager.SetColor(new Color32(85, 157, 255, 255));
            if (keyboard.digit0Key.wasPressedThisFrame) colorManager.SetColor(Color.white);
        }
        if (keyboard.mKey.wasPressedThisFrame) OpenDebugMenu(PalmMenuTab.Model);
        if (keyboard.nKey.wasPressedThisFrame) OpenDebugMenu(PalmMenuTab.Edit);
        if (keyboard.cKey.wasPressedThisFrame) OpenDebugMenu(PalmMenuTab.Color);
        if (keyboard.xKey.wasPressedThisFrame) OpenDebugMenu(PalmMenuTab.None);

        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (!shift) return;
        if (keyboard.fKey.wasPressedThisFrame) { LogAction("Paint Selected", paintTool != null && paintTool.PaintSelected()); }

        if (keyboard.hKey.wasPressedThisFrame) PressOptionalButton(colorPickerButton, "Color Picker");
        if (keyboard.aKey.wasPressedThisFrame)
        {
            if (hsvAddToPalette != null)
            {
                hsvAddToPalette.AddColor();
                LogExecuted("HSV Add To Palette");
            }
            else { Debug.LogWarning("[HSV DEBUG] HSVAddToPalette is not assigned!", this); }
        }
        if (keyboard.dKey.wasPressedThisFrame) PressOptionalButton(duplicateButton, "Duplicate");
        if (keyboard.deleteKey.wasPressedThisFrame) PressOptionalButton(deleteButton, "Delete");
        if (keyboard.yKey.wasPressedThisFrame) PressOptionalButton(groupButton, "Group");
        if (keyboard.zKey.wasPressedThisFrame) PressOptionalButton(ungroupButton, "Ungroup");
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
                if (keyboard.eKey.wasPressedThisFrame) LogAction("Eraser", eraserTool != null && eraserTool.EraseTarget());
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
                if (keyboard.gKey.wasPressedThisFrame) LogAction("Scale 2x", scaleTool != null && scaleTool.ScaleUniform2());
                break;
        }
    }

    private void ActivateTool(HandTool tool)
    {
        toolManager.SelectTool(tool);
        if (logActions) Debug.Log($"[XR Keyboard Debug] Activated: {tool}", this);
    }
    private void OpenDebugMenu(PalmMenuTab tab)
    {
        if (handMenuController == null) { Debug.LogWarning("[MENU DEBUG] HandMenuController is NOT assigned!", this); return; }

        switch (tab)
        {
            case PalmMenuTab.Model:
                handMenuController.OpenModelMenu();
                break;

            case PalmMenuTab.Edit:
                handMenuController.OpenEditMenu();
                break;

            case PalmMenuTab.Color:
                handMenuController.OpenColorPalette();
                break;

            case PalmMenuTab.None:
                handMenuController.PressBack();
                break;
        }

        Debug.Log($"[MENU DEBUG] Requested: {tab}", this);
    }
    private void LogAction(string action, bool success)
    {
        if (logActions) Debug.Log($"[XR Keyboard Debug] {action}: {(success ? "SUCCESS" : "FAILED")}", this);
    }

    private void LogExecuted(string action)
    {
        if (logActions) Debug.Log($"[XR Keyboard Debug] {action}: Executed", this);
    }
    private void PressOptionalButton(HandToolButton button, string action)
    {
        if (button == null) { Debug.LogWarning($"[XR Keyboard Debug] {action}: Button not assigned.", this); return; }
        button.Press();
        LogExecuted(action);
    }
}

#endif