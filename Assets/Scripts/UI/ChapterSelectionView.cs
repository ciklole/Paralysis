using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored chapter screen. The list itself is still
/// data-driven from ChapterDatabase: ChapterSelectionController clones the
/// inactive ChapterButtonTemplate once per chapter. Edit the template to
/// change the button look, font, background or hover motion.
/// </summary>
public class ChapterSelectionView : MonoBehaviour
{
    [Header("Root")]
    public RectTransform root;

    [Header("Text")]
    public Text titleText;
    public Text emptyText;

    [Header("List")]
    public RectTransform viewport;
    public RectTransform content;

    [Tooltip("Inactive button inside the content. It is cloned for each chapter.")]
    public GameObject chapterButtonTemplate;

    [Header("Navigation")]
    public MenuButton backButton;

    [Tooltip(
        "When on, MainMenuConfig overwrites the labels saved in the canvas. " +
        "Leave off to edit the wording directly in the Canvas.")]
    public bool applyConfigText;
}
