using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CustomPalettePagination : MonoBehaviour
{
    [Header("8 Reusable Custom Swatches")]
    [SerializeField] private ColorSwatchButton[] swatches = new ColorSwatchButton[8];

    [Header("Navigation Buttons")]
    [SerializeField] private XRSimpleInteractable previousButton;
    [SerializeField] private XRSimpleInteractable nextButton;

    [Header("Pagination")]
    [SerializeField] private int currentPage = 0;

    private const int ColorsPerPage = 8;
    private readonly List<Color> savedColors = new List<Color>();
    public int CurrentPage => currentPage;
    public int ColorCount => savedColors.Count;
    public int PageCount => Mathf.Max(1, (savedColors.Count + ColorsPerPage - 1) / ColorsPerPage);

    private void OnEnable()
    {
        if (previousButton != null) previousButton.selectEntered.AddListener(OnPrevious);
        if (nextButton != null) nextButton.selectEntered.AddListener(OnNext);

        RefreshPage();
    }

    private void OnDisable()
    {
        if (previousButton != null) previousButton.selectEntered.RemoveListener(OnPrevious);
        if (nextButton != null) nextButton.selectEntered.RemoveListener(OnNext);
    }

    private void OnPrevious(SelectEnterEventArgs args) { PreviousPage(); }
    private void OnNext(SelectEnterEventArgs args) { NextPage(); }
    public void PreviousPage() { ShowPage(currentPage - 1); }
    public void NextPage() { ShowPage(currentPage + 1); }
    public void ShowPage(int pageIndex)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, PageCount - 1);
        RefreshPage();
    }

    public void AddColor(Color newColor)
    {
        savedColors.Add(newColor);
        currentPage = (savedColors.Count - 1) / ColorsPerPage;

        RefreshPage();

        Debug.Log($"[Custom Palette] Added color " + $"#{ColorUtility.ToHtmlStringRGB(newColor)} " + $"| Total: {savedColors.Count} " + $"| Page: {currentPage + 1}/{PageCount}");
    }

    private void RefreshPage()
    {
        if (swatches == null) return;
        for (int i = 0; i < swatches.Length; i++)
        {
            ColorSwatchButton swatch = swatches[i];

            if (swatch == null) continue;
            int colorIndex = currentPage * ColorsPerPage + i;
            bool hasColor = colorIndex < savedColors.Count;
            if (hasColor) { swatch.SetSwatchColor(savedColors[colorIndex]); }

            swatch.gameObject.SetActive(hasColor);
        }
    }

    [ContextMenu("Test - Next Page")]
    private void TestNextPage() { NextPage(); }

    [ContextMenu("Test - Previous Page")]
    private void TestPreviousPage() {  PreviousPage(); }
}