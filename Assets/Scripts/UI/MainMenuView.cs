using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored main-menu canvas. The builder creates this
/// component and wires every field, but the hierarchy is a normal Canvas in
/// MainMenu.unity: text, images, buttons and panels can all be edited by hand.
///
/// When this component is present, MainMenuController binds to it instead of
/// building a second UI at runtime.
/// </summary>
public class MainMenuView : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform menuContent;
    public Image backgroundImage;

    [Header("Text")]
    public Text titleText;
    public Text statusText;

    [Header("Buttons")]
    public MenuButton newGameButton;
    public MenuButton continueButton;
    public MenuButton chaptersButton;
    public MenuButton settingsButton;
    public MenuButton quitButton;

    [Header("Panels")]
    public SettingsPanel settingsPanel;
    public ChapterSelectionController chapterSelection;

    [Header("Audio")]
    public MenuAudio menuAudio;

    [Tooltip(
        "When on, the labels in MainMenuConfig overwrite the text saved in " +
        "the canvas. Leave off to edit the wording directly in the Canvas.")]
    public bool applyConfigText;
}
