using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored pause canvas. PauseManager still drives the
/// same slide/fade animation, but it moves these objects instead of creating
/// them, so the split screen can be edited in the PauseMenu prefab.
/// </summary>
public class PauseMenuView : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform canvasRect;

    [Header("Split Screen")]
    public RectTransform leftPanel;
    public Image leftImage;
    public Image rightOverlay;

    [Header("Animated Content")]
    public RectTransform contentGroup;
    public CanvasGroup contentCanvasGroup;
    public RectTransform contentShift;
    public RectTransform buttonsRoot;
    public Text titleText;

    [Header("Buttons")]
    public MenuButton continueButton;
    public MenuButton settingsButton;
    public MenuButton replayButton;
    public MenuButton mainMenuButton;

    [Header("Settings")]
    public SettingsPanel settingsPanel;

    [Header("Audio")]
    public MenuAudio menuAudio;

    [Tooltip(
        "Use the colours saved on the canvas images. Turn off to use the " +
        "PauseMenuConfig colours instead.")]
    public bool useAuthoredColors = true;
}
