using UnityEngine;

/// <summary>
/// Layout and text for one settings screen. The main menu and the pause menu
/// each own an instance of this class, so their layouts can differ while the
/// underlying values and persistence stay shared.
/// </summary>
[System.Serializable]
public class SettingsPanelConfig
{
    [Header("Text")]
    public bool showTitle = true;
    public string titleText = "SETTINGS";
    public string sensitivityLabel = "MOUSE SENSITIVITY";
    public string frameRateLabel = "FRAME RATE";
    public string backLabel = "BACK";
    public string fpsFormat = "{0} FPS";
    public string valueFormat = "{0}";

    [Header("Styles")]
    public TextStyle titleStyle = new TextStyle
    {
        fontSize = 48,
        bold = true,
        color = Color.white,
        alignment = TextAnchor.MiddleCenter
    };

    public TextStyle labelStyle = new TextStyle
    {
        fontSize = 28,
        bold = false,
        color = new Color(0.72f, 0.72f, 0.72f, 1f),
        alignment = TextAnchor.MiddleLeft
    };

    public TextStyle valueStyle = new TextStyle
    {
        fontSize = 26,
        bold = true,
        color = Color.white,
        alignment = TextAnchor.MiddleRight
    };

    public TextStyle buttonTextStyle = new TextStyle
    {
        fontSize = 26,
        bold = false,
        color = new Color(0.62f, 0.62f, 0.62f, 1f),
        alignment = TextAnchor.MiddleCenter
    };

    public MenuButtonStyle optionButtonStyle = new MenuButtonStyle
    {
        hoverOffset = 4f,
        duration = 0.12f,
        normalFontSize = 26,
        hoverFontSize = 28,
        normalColor = new Color(0.62f, 0.62f, 0.62f, 1f),
        hoverColor = Color.white,
        normalBold = false,
        hoverBold = true
    };

    public MenuButtonStyle backButtonStyle = new MenuButtonStyle
    {
        hoverOffset = 8f,
        duration = 0.15f,
        normalFontSize = 30,
        hoverFontSize = 34,
        normalColor = new Color(0.72f, 0.72f, 0.72f, 1f),
        hoverColor = Color.white,
        normalBold = false,
        hoverBold = true
    };

    [Header("Layout")]
    public float panelWidth = 900f;
    public float panelHeight = 650f;
    public Vector2 panelAnchoredPosition = Vector2.zero;
    public Vector2 padding = new Vector2(70f, 60f);
    public float rowSpacing = 18f;
    public float titleHeight = 90f;
    public float labelHeight = 40f;
    public float sliderHeight = 28f;
    public float valueWidth = 110f;
    public float optionWidth = 120f;
    public float optionHeight = 54f;
    public float optionSpacing = 12f;
    public float backWidth = 240f;
    public float backHeight = 60f;
    public float backTopSpacing = 24f;

    [Header("Slider Colours")]
    public Color sliderBackground = new Color(0.16f, 0.16f, 0.16f, 1f);
    public Color sliderFill = Color.white;
    public Color sliderHandle = Color.white;
}
