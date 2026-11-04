using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored instruction toast. ToastUI still owns the
/// message and timer, but it writes into this Canvas instead of building one,
/// so the text, panel and placement can be edited as a prefab.
/// </summary>
public class ToastView : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform panel;

    [Header("Text")]
    public Text text;

    [Header("Optional")]
    public CanvasGroup panelGroup;

    [Tooltip(
        "Apply the ToastUI TextStyle. Leave off to keep the font, size and " +
        "colour authored in the Canvas.")]
    public bool applyStyles;
}
