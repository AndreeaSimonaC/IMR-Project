#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;

public class EditorVoxelToolInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera testCamera;
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private SelectTool selectTool;
    [SerializeField] private EraserTool eraserTool;
    [SerializeField] private MoveTool moveTool;
    [SerializeField] private RotateTool rotateTool;
    [SerializeField] private ScaleTool scaleTool;

    [Header("Raycast")]
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private float maxDistance = 100f;

    private void Start()
    {
        if (testCamera == null) testCamera = Camera.main;

        if (testCamera == null || toolManager == null || selectTool == null || eraserTool == null || moveTool == null || rotateTool == null || scaleTool == null)
        {
            Debug.LogError("[EditorVoxelToolInput] Missing references.", this);

            enabled = false;
            return;
        }

        Debug.Log("[EditorVoxelToolInput] Sandbox controls ready.", this);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        if (mouse == null || keyboard == null || toolManager == null) return;

        HandTool activeTool = toolManager.ActiveTool;

        if (activeTool == HandTool.Select || activeTool == HandTool.Eraser)
        {
            UpdateMouseTarget(mouse, keyboard, activeTool);
        }
        else
        {
            selectTool.ClearTarget();
            eraserTool.ClearTarget();
        }

        switch (activeTool)
        {
            case HandTool.Eraser:
                if (keyboard.deleteKey.wasPressedThisFrame) eraserTool.EraseSelection();
                break;

            case HandTool.Move:
                HandleMove(keyboard);
                break;

            case HandTool.Rotate:
                HandleRotate(keyboard);
                break;

            case HandTool.Scale:
                HandleScale(keyboard);
                break;
        }
    }

    private void UpdateMouseTarget(Mouse mouse, Keyboard keyboard, HandTool activeTool)
    {
        Ray ray = testCamera.ScreenPointToRay(mouse.position.ReadValue());

        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayers, QueryTriggerInteraction.Ignore);

        if (activeTool == HandTool.Select)
        {
            eraserTool.ClearTarget();

            if (hasHit) selectTool.UpdateTarget(hit);
            else selectTool.ClearTarget();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                bool additive = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                selectTool.SelectTarget(additive);
                Debug.Log("[Sandbox] Select click | " + $"Additive: {additive}", this);
            }
        }
        else if (activeTool == HandTool.Eraser)
        {
            selectTool.ClearTarget();

            if (hasHit) eraserTool.UpdateTarget(hit);
            else eraserTool.ClearTarget();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                bool erased = eraserTool.EraseTarget();
                Debug.Log($"[Sandbox] Erase click | Success: {erased}", this);
            }
        }
    }


    private void HandleMove(Keyboard keyboard)
    {
        if (keyboard.rightArrowKey.wasPressedThisFrame)
            moveTool.MoveRight();

        if (keyboard.leftArrowKey.wasPressedThisFrame)
            moveTool.MoveLeft();

        if (keyboard.upArrowKey.wasPressedThisFrame)
            moveTool.MoveForward();

        if (keyboard.downArrowKey.wasPressedThisFrame)
            moveTool.MoveBackward();

        // Move UP: Page Up or E
        if (keyboard.pageUpKey.wasPressedThisFrame ||
            keyboard.eKey.wasPressedThisFrame)
        {
            Debug.Log("[Move Test] UP pressed");
            moveTool.MoveUp();
        }

        // Move DOWN: Page Down or Q
        if (keyboard.pageDownKey.wasPressedThisFrame ||
            keyboard.qKey.wasPressedThisFrame)
        {
            Debug.Log("[Move Test] DOWN pressed");
            moveTool.MoveDown();
        }
    }


    private void HandleRotate(Keyboard keyboard)
    {
        if (keyboard.rKey.wasPressedThisFrame) rotateTool.RotateY();
        if (keyboard.tKey.wasPressedThisFrame) rotateTool.RotateX();
        if (keyboard.yKey.wasPressedThisFrame) rotateTool.RotateZ();
    }

    private void HandleScale(Keyboard keyboard)
    {
        if (keyboard.iKey.wasPressedThisFrame) scaleTool.ScaleX2();
        if (keyboard.oKey.wasPressedThisFrame) scaleTool.ScaleY2();
        if (keyboard.pKey.wasPressedThisFrame) scaleTool.ScaleZ2();
        if (keyboard.uKey.wasPressedThisFrame) scaleTool.ScaleUniform2();
    }

    private void OnDisable()
    {
        if (selectTool != null) selectTool.ClearTarget();
        if (eraserTool != null) eraserTool.ClearTarget();
    }
}

#endif