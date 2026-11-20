using System;
using UnityEngine;
public enum PalmMenuTab { None, Model, Edit, Color }

public class HandMenuController : MonoBehaviour
{
    [Header("Palm Menus")]
    [SerializeField] private GameObject leftPalmModelMenu;
    [SerializeField] private GameObject rightPalmEditMenu;
    [SerializeField] private GameObject rightPalmColorMenu;
    [SerializeField] private GameObject leftPalmColorPalette;

    public PalmMenuTab ActiveTab { get; private set; } = PalmMenuTab.None;
    public event Action<PalmMenuTab> OnTabChanged;

    private void Start()
    {
        CloseAllMenus();
        Debug.Log("HandMenuController: Initialized.", this);
    }

    public void OpenModelMenu() { SetTab(PalmMenuTab.Model); }

    public void OpenEditMenu() { SetTab(PalmMenuTab.Edit); }

    public void OpenColorMenu() { SetTab(PalmMenuTab.Color); }

    public void OpenColorPalette()
    {
        bool wasColorOpen = ActiveTab == PalmMenuTab.Color;
        bool paletteWasOpen = leftPalmColorPalette != null && leftPalmColorPalette.activeSelf;
        SetTab(PalmMenuTab.Color);
        if (leftPalmColorPalette != null) leftPalmColorPalette.SetActive(!wasColorOpen || !paletteWasOpen);
    }

    public void CloseAllMenus()
    {
        SetActive(leftPalmModelMenu, false);
        SetActive(rightPalmEditMenu, false);
        SetActive(rightPalmColorMenu, false);
        SetActive(leftPalmColorPalette, false);

        ActiveTab = PalmMenuTab.None;
        OnTabChanged?.Invoke(ActiveTab);

        Debug.Log("HandMenuController: All menus closed.", this);
    }

    public void PressBack() { CloseAllMenus(); }

    private void SetTab(PalmMenuTab tab)
    {
        if (tab == PalmMenuTab.None) { CloseAllMenus(); return; }
        if (ActiveTab == tab) return;

        ActiveTab = tab;

        SetActive(leftPalmModelMenu, tab == PalmMenuTab.Model);
        SetActive(rightPalmEditMenu, tab == PalmMenuTab.Edit);
        SetActive(rightPalmColorMenu, tab == PalmMenuTab.Color);
        SetActive(leftPalmColorPalette, false);

        OnTabChanged?.Invoke(ActiveTab);
        Debug.Log($"[Palm Menu] Active tab: {tab}", this);
    }

    private void SetActive(GameObject target, bool value) { if (target != null) target.SetActive(value); }

    [ContextMenu("TEST - Open Model")]
    private void TestModel() => OpenModelMenu();

    [ContextMenu("TEST - Open Edit")]
    private void TestEdit() => OpenEditMenu();

    [ContextMenu("TEST - Open Color")]
    private void TestColor() => OpenColorMenu();

    [ContextMenu("TEST - Open Color Palette")]
    private void TestPalette() => OpenColorPalette();
}