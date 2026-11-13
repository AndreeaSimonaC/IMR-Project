
using System;
using UnityEngine;

public class ToolManager : MonoBehaviour
{
    [Header("Active Tool")]
    [SerializeField] private HandTool activeTool = HandTool.Brush;

    public HandTool ActiveTool => activeTool;

    public event Action<HandTool> OnToolChanged;

    private void Start()
    {
        Debug.Log($"ToolManager initialized. Active tool: {activeTool}", this);
    }

    public void SelectTool(HandTool newTool)
    {
        if (activeTool == newTool)
        {
            Debug.Log($"Tool already active: {newTool}", this);
            return;
        }

        HandTool previousTool = activeTool;
        activeTool = newTool;

        Debug.Log($"Tool changed: {previousTool} -> {activeTool}", this);

        OnToolChanged?.Invoke(activeTool);
    }
}
