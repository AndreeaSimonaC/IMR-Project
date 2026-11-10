
using UnityEngine;

public class HandMenuController : MonoBehaviour
{
    [Header("Palm Menus")]
    [SerializeField] private GameObject leftPalmModelMenu;
    [SerializeField] private GameObject rightPalmEditMenu;
    [SerializeField] private GameObject rightPalmColorMenu;

    private void Start()
    {
        Debug.Log("HandMenuController: Initialized.", this);
        CloseAllMenus();
    }

    public void OpenModelMenu()
    {
        Debug.Log("LEFT HAND - Model label pressed!", this);
        ShowMenu(leftPalmModelMenu);
    }

    public void OpenEditMenu()
    {
        Debug.Log("LEFT HAND - Edit label pressed!", this);
        ShowMenu(rightPalmEditMenu);
    }

    public void OpenColorMenu()
    {
        Debug.Log("LEFT HAND - Color label pressed!", this);
        ShowMenu(rightPalmColorMenu);
    }

    public void CloseAllMenus()
    {
        if (leftPalmModelMenu != null) leftPalmModelMenu.SetActive(false);
        if (rightPalmEditMenu != null) rightPalmEditMenu.SetActive(false);
        if (rightPalmColorMenu != null) rightPalmColorMenu.SetActive(false);
        Debug.Log("HandMenuController: All palm menus closed.", this);
    }

    public void PressBack()
    {
        Debug.Log("BACK button pressed!", this);
        CloseAllMenus();
    }

    private void ShowMenu(GameObject selectedMenu)
    {
        if (selectedMenu == null)
        {
            Debug.LogWarning("HandMenuController: Menu reference missing!", this);
            return;
        }

        bool shouldOpen = !selectedMenu.activeSelf;
        CloseAllMenus();
        selectedMenu.SetActive(shouldOpen);
        Debug.Log(shouldOpen ? $"MENU OPENED: {selectedMenu.name}" : $"MENU CLOSED: {selectedMenu.name}", this);
    }

    [ContextMenu("TEST - Open Model")]
    private void TestModel() { OpenModelMenu();}
    [ContextMenu("TEST - Open Edit")] 
    private void TestEdit() { OpenEditMenu(); }

    [ContextMenu("TEST - Open Color")]
    private void TestColor() { OpenColorMenu(); }
}
