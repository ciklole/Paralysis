using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored dialogue canvas. DialogueSystem still owns
/// the typewriter, queue, input and voice playback, but it reads/writes these
/// objects instead of creating a UI. Add portraits, images or extra text
/// freely; they are left alone.
/// </summary>
public class DialogueView : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform panel;
    public CanvasGroup panelGroup;

    [Header("Text")]
    public RectTransform speakerRect;
    public Text speakerText;
    public RectTransform bodyRect;
    public Text bodyText;

    [Header("Optional")]
    public Image portraitImage;
    public GameObject advancePrompt;

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioSource sfxSource;

    [Tooltip(
        "When on, DialogueSystem applies the speaker/body TextStyle from the " +
        "scene component. Leave off to keep the font and colours authored " +
        "in the Canvas.")]
    public bool applyStyles;

    [Tooltip(
        "Apply the speaker palette (and per-line speakerColor) even when the " +
        "Canvas owns the base font and colour.")]
    public bool applySpeakerColors = true;

    [Tooltip(
        "When on, DialogueSystem recalculates the body position/size for the " +
        "speaker line. Leave off to keep the layout authored in the Canvas.")]
    public bool applyRuntimeLayout;
}
