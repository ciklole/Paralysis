using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to a hand-authored chapter-introduction layout.
/// ChapterIntroScreen still owns the fade timing and per-chapter text, but it
/// updates these objects instead of creating them.
/// </summary>
public class ChapterIntroView : MonoBehaviour
{
    [Header("Root")]
    public RectTransform root;
    public CanvasGroup group;

    [Header("Text")]
    public Text numberText;
    public Text titleText;
    public Text subtitleText;

    [Tooltip(
        "When on, the ChapterIntroStyle from ChapterDatabase also overrides " +
        "the font, colour, size and position of the three texts. Leave off " +
        "to keep the layout authored in the Canvas.")]
    public bool applyChapterStyles;
}
