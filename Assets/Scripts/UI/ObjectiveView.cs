using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored objective HUD. ObjectiveManager still owns
/// the objective list and show/hide timing, but it updates these objects
/// instead of creating a Canvas, so the HUD can be edited as a prefab.
/// </summary>
public class ObjectiveView : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform panel;

    [Header("Text")]
    public Text headerText;
    public Text bodyText;

    [Header("Colours")]
    [Tooltip("Colour used for a completed objective.")]
    public Color completedColor = new Color(0.72f, 0.72f, 0.72f, 1f);

    [Tooltip(
        "Apply the ObjectiveManager header/body TextStyle. Leave off to keep " +
        "the font, size and colours authored in the Canvas.")]
    public bool applyStyles;
}
