
using UnityEngine;

public static class ProjectSettings
{
    public static int Width { get; private set; } = 32;
    public static int Height { get; private set; } = 32;
    public static int Length { get; private set; } = 32;
    public static bool HasProject { get; private set; } = false;
    public static void SetDimensions(int width, int height, int length)
    {
        Width = Mathf.Clamp(width, 1, 64);
        Height = Mathf.Clamp(height, 1, 64);
        Length = Mathf.Clamp(length, 1, 64);
        HasProject = true;
        Debug.Log($"ProjectSettings: Dimensions saved: {Width} x {Height} x {Length}");
    }
}