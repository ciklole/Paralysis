using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to the hand-authored loading/transition canvas. SceneTransition
/// Manager keeps the same fade and chapter-intro flow, but operates on these
/// objects instead of building them.
/// </summary>
public class TransitionView : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform canvasRect;

    [Header("Loading")]
    public RectTransform blackRoot;
    public CanvasGroup blackGroup;
    public Text loadingText;
    public LoadingIndicator loadingIndicator;

    [Tooltip(
        "Use the text saved in the LoadingText object instead of the " +
        "SceneFlowConfig label.")]
    public bool useAuthoredLoadingText = true;

    [Header("Chapter Intro")]
    public ChapterIntroScreen introScreen;

    [Header("Audio")]
    public AudioSource audioSource;
}
