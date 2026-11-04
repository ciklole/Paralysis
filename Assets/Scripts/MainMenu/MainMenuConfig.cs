using UnityEngine;

/// <summary>
/// Inspector-editable content, typography, layout and audio for the main
/// menu, the chapter list and the shared settings screen. The scene only
/// contains the controller and a camera; everything else is built from this
/// asset at runtime.
/// </summary>
[CreateAssetMenu(
    fileName = "MainMenuConfig",
    menuName = "Paralysis/Main Menu Config")]
public class MainMenuConfig : ScriptableObject
{
    [Header("Background")]
    public Color backgroundColor = Color.black;

    [Tooltip("Optional full-screen picture behind the menu.")]
    public Sprite backgroundSprite;

    [Header("Title")]
    public bool showTitle = true;
    public string titleText = "PARALYSIS";

    public TextStyle titleStyle = new TextStyle
    {
        fontSize = 104,
        bold = true,
        color = Color.white,
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 titleOffset = new Vector2(0f, 92f);
    public Vector2 titleSize = new Vector2(1500f, 180f);

    [Header("Buttons")]
    public string newGameLabel = "NEW GAME";
    public string continueLabel = "CONTINUE";
    public string chaptersLabel = "CHAPTERS";
    public string settingsLabel = "SETTINGS";
    public string quitLabel = "QUIT GAME";

    public TextStyle buttonTextStyle = new TextStyle
    {
        fontSize = 38,
        bold = false,
        color = new Color(0.72f, 0.72f, 0.72f, 1f),
        alignment = TextAnchor.MiddleCenter
    };

    public MenuButtonStyle buttonStyle = new MenuButtonStyle
    {
        hoverOffset = 11f,
        duration = 0.17f,
        normalFontSize = 38,
        hoverFontSize = 44,
        normalColor = new Color(0.72f, 0.72f, 0.72f, 1f),
        hoverColor = Color.white,
        normalBold = false,
        hoverBold = true
    };

    public Vector2 buttonStart = new Vector2(0f, 120f);
    public Vector2 buttonSize = new Vector2(560f, 66f);
    public float buttonSpacing = 18f;

    [Header("Continue Availability")]
    public string noSaveLabel = "NO SAVE FOUND";
    public TextStyle secondaryTextStyle = new TextStyle
    {
        fontSize = 17,
        bold = false,
        color = new Color(0.55f, 0.55f, 0.55f, 1f),
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 secondaryOffset = new Vector2(0f, -30f);
    public Vector2 secondarySize = new Vector2(560f, 24f);

    [Header("Status Message")]
    public TextStyle statusStyle = new TextStyle
    {
        fontSize = 20,
        bold = false,
        color = new Color(0.9f, 0.45f, 0.4f, 1f),
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 statusOffset = new Vector2(0f, 44f);
    public Vector2 statusSize = new Vector2(1100f, 40f);
    public float statusDuration = 3.5f;

    [Header("Chapter Screen")]
    public string chapterScreenTitle = "CHAPTERS";

    public TextStyle chapterScreenTitleStyle = new TextStyle
    {
        fontSize = 52,
        bold = true,
        color = Color.white,
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 chapterTitleOffset = new Vector2(0f, 88f);
    public Vector2 chapterTitleSize = new Vector2(1200f, 100f);

    public Vector2 chapterListSize = new Vector2(1040f, 560f);
    public Vector2 chapterListOffset = new Vector2(0f, -30f);

    public string chapterLabelFormat = "CHAPTER {0}   {1}";
    public string chapterLockedSuffix = "   [LOCKED]";
    public string chapterEmptyLabel = "NO CHAPTERS CONFIGURED";

    public TextStyle chapterEmptyStyle = new TextStyle
    {
        fontSize = 24,
        bold = false,
        color = new Color(0.6f, 0.6f, 0.6f, 1f),
        alignment = TextAnchor.MiddleCenter
    };

    public TextStyle chapterButtonTextStyle = new TextStyle
    {
        fontSize = 30,
        bold = false,
        color = new Color(0.72f, 0.72f, 0.72f, 1f),
        alignment = TextAnchor.MiddleLeft
    };

    public MenuButtonStyle chapterButtonStyle = new MenuButtonStyle
    {
        hoverOffset = 7f,
        duration = 0.15f,
        normalFontSize = 30,
        hoverFontSize = 34,
        normalColor = new Color(0.72f, 0.72f, 0.72f, 1f),
        hoverColor = Color.white,
        normalBold = false,
        hoverBold = true
    };

    public float chapterButtonHeight = 68f;
    public float chapterButtonSpacing = 12f;
    public Color chapterBackdrop = new Color(0f, 0f, 0f, 0.72f);

    public string chapterBackLabel = "BACK";

    public TextStyle chapterBackTextStyle = new TextStyle
    {
        fontSize = 28,
        bold = false,
        color = new Color(0.72f, 0.72f, 0.72f, 1f),
        alignment = TextAnchor.MiddleCenter
    };

    public MenuButtonStyle chapterBackStyle = new MenuButtonStyle
    {
        hoverOffset = 8f,
        duration = 0.15f,
        normalFontSize = 28,
        hoverFontSize = 32,
        normalColor = new Color(0.72f, 0.72f, 0.72f, 1f),
        hoverColor = Color.white,
        normalBold = false,
        hoverBold = true
    };

    public Vector2 chapterBackOffset = new Vector2(0f, 80f);
    public Vector2 chapterBackSize = new Vector2(260f, 58f);

    [Header("Settings Screen")]
    public SettingsPanelConfig settingsPanel = new SettingsPanelConfig
    {
        showTitle = true,
        titleText = "SETTINGS",
        panelWidth = 900f,
        panelHeight = 640f,
        padding = new Vector2(70f, 55f),
        titleHeight = 90f
    };

    [Header("Chapter Intro")]
    public bool showChapterIntroOnNewGame = true;
    public bool showChapterIntroOnContinue = true;
    public bool showChapterIntroOnChapterSelect = true;

    [Header("Audio")]
    public MenuAudioConfig audio = new MenuAudioConfig();

    private void OnValidate()
    {
        buttonSize.x = Mathf.Max(80f, buttonSize.x);
        buttonSize.y = Mathf.Max(30f, buttonSize.y);
        buttonSpacing = Mathf.Max(0f, buttonSpacing);
        titleSize.x = Mathf.Max(80f, titleSize.x);
        titleSize.y = Mathf.Max(20f, titleSize.y);
        chapterButtonHeight = Mathf.Max(30f, chapterButtonHeight);
        chapterButtonSpacing = Mathf.Max(0f, chapterButtonSpacing);
        statusDuration = Mathf.Max(0.5f, statusDuration);
    }
}
