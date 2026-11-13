
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewProjectSetup : MonoBehaviour
{
    [SerializeField] private NumberStepper widthStepper;
    [SerializeField] private NumberStepper heightStepper;
    [SerializeField] private NumberStepper lengthStepper;
    [SerializeField] private string modelingScene = "Modeling_Main";
    private const int MaxDimension = 128;

    public void StartProject()
    {
        if (widthStepper == null || heightStepper == null || lengthStepper == null)
        {
            Debug.LogWarning("NewProjectSetup: One or more steppers are not assigned!", this);
            return;
        }

        int width = widthStepper.GetValue();
        int height = heightStepper.GetValue();
        int length = lengthStepper.GetValue();

        if (width < 1 || height < 1 || length < 1 || width > MaxDimension || height > MaxDimension || length > MaxDimension)
        {
            Debug.LogWarning($"NewProjectSetup: Invalid workspace dimensions: {width} x {height} x {length}. " + $"Each dimension must be between 1 and {MaxDimension}.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(modelingScene))
        {
            Debug.LogError($"NewProjectSetup: Scene '{modelingScene}' is not available in Build Profiles.", this);
            return;
        }
        ProjectSettings.SetDimensions(width, height, length);
        Debug.Log($"Starting project: {width} x {height} x {length}");
        SceneManager.LoadScene(modelingScene);
    }
}