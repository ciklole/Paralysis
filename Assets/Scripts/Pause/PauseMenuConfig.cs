using UnityEngine;

/// <summary>
/// Inspector-editable layout, wording and motion for the pause menu. The menu
/// is split in two: a solid black panel on the left with the text buttons, and
/// a translucent dark layer over the still-visible game on the right.
/// </summary>
[CreateAssetMenu(
    fileName = "PauseMenuConfig",
    menuName = "Paralysis/Pause Menu Config")]
public class PauseMenuConfig : ScriptableObject
{
    [Header("Input")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Tooltip("Also pause when a dialogue or inspection window is open.")]
    public bool allowDuringModals = false;

    [Header("Left Panel")]
    [Range(0.2f, 0.8f)]
    public float leftWidthFraction = 0.42f;

    public Color leftPanelColor = Color.black;

    [Header("Right Side")]
    [Tooltip(
        "Darkens the live gameplay on the right without hiding it. Alpha is " +
        "the strength of the darkening.")]
    public Color rightDarkening = new Color(0f, 0f, 0f, 0.62f);

    [Header("Title")]
    public bool showTitle = true;
    public string titleText = "PAUSED";

    public TextStyle titleStyle = new TextStyle
    {
        fontSize = 24,
        bold = false,
        color = new Color(0.62f, 0.62f, 0.62f, 1f),
        alignment = TextAnchor.MiddleLeft
    };

    public Vector2 titleOffset = new Vector2(120f, 150f);
    public Vector2 titleSize = new Vector2(500f, 50f);

    [Header("Buttons")]
    public string continueLabel = "CONTINUE";
    public string settingsLabel = "SETTINGS";
    public string replayLabel = "REPLAY CHAPTER";
    public string mainMenuLabel = "MAIN MENU";

    public TextStyle buttonTextStyle = new TextStyle
    {
        fontSize = 40,
        bold = false,
        color = new Color(0.72f, 0.72f, 0.72f, 1f),
        alignment = TextAnchor.MiddleLeft
    };

    public MenuButtonStyle buttonStyle = new MenuButtonStyle
    {
        hoverOffset = 9f,
        duration = 0.16f,
        normalFontSize = 40,
        hoverFontSize = 46,
        normalColor = new Color(0.72f, 0.72f, 0.72f, 1f),
        hoverColor = Color.white,
        normalBold = false,
        hoverBold = true
    };

    public Vector2 buttonOffset = new Vector2(120f, 320f);
    public Vector2 buttonSize = new Vector2(560f, 70f);
    public float buttonSpacing = 24f;

    [Header("Settings Panel")]
    public SettingsPanelConfig settingsPanel = new SettingsPanelConfig
    {
        showTitle = true,
        titleText = "SETTINGS",
        panelWidth = 0f,
        panelHeight = 0f,
        padding = new Vector2(120f, 170f),
        titleHeight = 90f,
        backWidth = 260f,
        backHeight = 60f,
        backTopSpacing = 26f
    };

    [Header("Animation")]
    [Tooltip("Seconds for the black panel to slide in.")]
    public float openDuration = 0.34f;

    [Tooltip("Seconds for the panel to slide away before gameplay resumes.")]
    public float closeDuration = 0.24f;

    [Tooltip("Easing applied to the slide and fade.")]
    public AnimationCurve easing =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("How far the button block slides while the menu opens.")]
    public float contentSlide = 48f;

    [Header("Behaviour")]
    [Tooltip("Show the chapter introduction when REPLAY CHAPTER is chosen.")]
    public bool replayShowsChapterIntro = true;

    [Header("Audio")]
    public MenuAudioConfig audio = new MenuAudioConfig();

    [Header("Authored Canvas")]
    [Tooltip(
        "Prefab with a PauseMenuView. When assigned, the pause menu uses " +
        "this editable Canvas instead of building its UI at runtime.")]
    public GameObject canvasPrefab;

    private void OnValidate()
    {
        leftWidthFraction = Mathf.Clamp(leftWidthFraction, 0.2f, 0.8f);
        openDuration = Mathf.Max(0f, openDuration);
        closeDuration = Mathf.Max(0f, closeDuration);
        contentSlide = Mathf.Max(0f, contentSlide);
        buttonSpacing = Mathf.Max(0f, buttonSpacing);
        buttonSize.x = Mathf.Max(80f, buttonSize.x);
        buttonSize.y = Mathf.Max(30f, buttonSize.y);
        titleSize.x = Mathf.Max(80f, titleSize.x);
        titleSize.y = Mathf.Max(20f, titleSize.y);
    }
}
