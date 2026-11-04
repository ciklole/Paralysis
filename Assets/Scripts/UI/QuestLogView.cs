using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored mission log. QuestLogUI still owns the
/// objective/shopping text and input, but it shows this Canvas instead of
/// building one, so the whole log can be edited as a prefab.
/// </summary>
public class QuestLogView : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform panel;

    [Header("Text")]
    public Text titleText;
    public Text bodyText;

    [Header("Scrolling")]
    public ScrollRect scrollRect;

    [Tooltip(
        "Apply the QuestLogUI title/body TextStyle. Leave off to keep the " +
        "font, size and colours authored in the Canvas.")]
    public bool applyStyles;
}
