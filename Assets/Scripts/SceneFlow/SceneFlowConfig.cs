using UnityEngine;

/// <summary>
/// Inspector-editable presentation and timing for scene transitions. Stored
/// in Resources so the persistent transition manager can load it from any
/// scene, including the main menu.
/// </summary>
[CreateAssetMenu(
    fileName = "SceneFlowConfig",
    menuName = "Paralysis/Scene Flow Config")]
public class SceneFlowConfig : ScriptableObject
{
    [Header("Scenes")]
    public string mainMenuScene = "MainMenu";

    [Header("Loading Screen")]
    public string loadingText = "LOADING";

    public TextStyle loadingStyle = new TextStyle
    {
        fontSize = 48,
        bold = true,
        color = Color.white,
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 loadingAnchor = new Vector2(0.5f, 0.5f);
    public Vector2 loadingAnchoredPosition = Vector2.zero;
    public Vector2 loadingSize = new Vector2(1600f, 200f);

    public bool animateDots = true;
    public float dotStepInterval = 0.25f;

    [Header("Timing")]
    public float fadeInDuration = 0.35f;
    public float fadeOutDuration = 0.5f;

    [Tooltip(
        "The loading screen stays visible for at least this long. Keep this " +
        "short; it only prevents a one-frame flash on very fast loads.")]
    public float minimumLoadingDuration = 0.2f;

    [Tooltip("Abort the transition if the scene has not loaded in this time.")]
    public float loadingTimeout = 30f;

    [Header("Chapter Introduction")]
    public ChapterIntroStyle defaultIntroStyle = new ChapterIntroStyle();

    [Header("Cursor")]
    public bool hideCursorDuringTransitions = true;

    [Header("Audio")]
    public AudioClip transitionSound;

    [Range(0f, 1f)]
    public float transitionVolume = 0.25f;

    [Header("Authored Canvas")]
    [Tooltip(
        "Prefab with a TransitionView. When assigned, the loading screen " +
        "and chapter introduction use this editable Canvas.")]
    public GameObject transitionPrefab;
}
