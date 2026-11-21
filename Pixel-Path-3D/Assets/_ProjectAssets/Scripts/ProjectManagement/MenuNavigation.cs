
using UnityEngine;

public class MenuNavigation : MonoBehaviour
{
    [SerializeField] private GameObject getStartedPanel;
    [SerializeField] private GameObject selectModulePanel;
    [SerializeField] private GameObject selectProjectPanel;
    [SerializeField] private GameObject userInputCanvasSizes;

    private void Start()
    {
        ShowGetStartedPanel();
    }

    private void ShowPanel(GameObject panelToShow)
    {
        getStartedPanel.SetActive(panelToShow == getStartedPanel);
        selectModulePanel.SetActive(panelToShow == selectModulePanel);
        selectProjectPanel.SetActive(panelToShow == selectProjectPanel);
        userInputCanvasSizes.SetActive(panelToShow == userInputCanvasSizes);
    }

    public void ShowGetStartedPanel()
    {
        ShowPanel(getStartedPanel);
    }

    public void ShowSelectModulePanel()
    {
        ShowPanel(selectModulePanel);
    }

    public void ShowSelectProjectPanel()
    {
        ShowPanel(selectProjectPanel);
    }

    public void ShowUserInputCanvasSizes()
    {
        ShowPanel(userInputCanvasSizes);
    }
}
