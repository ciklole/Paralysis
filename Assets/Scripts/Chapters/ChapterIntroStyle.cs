using UnityEngine;

/// <summary>
/// Presentation for the black chapter-introduction screen. Each chapter can
/// use the database default or override every value here.
/// </summary>
[System.Serializable]
public class ChapterIntroStyle
{
    [Header("Chapter Number")]
    [Tooltip("Use {0} where the chapter number should appear.")]
    public string numberFormat = "CHAPTER {0}";

    public TextStyle numberStyle = new TextStyle
    {
        fontSize = 64,
        bold = true,
        color = Color.white,
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 numberAnchor = new Vector2(0.5f, 0.5f);
    public Vector2 numberAnchoredPosition = new Vector2(0f, 130f);
    public Vector2 numberSize = new Vector2(1400f, 120f);

    [Header("Title")]
    public TextStyle titleStyle = new TextStyle
    {
        fontSize = 96,
        bold = true,
        color = Color.white,
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 titleAnchor = new Vector2(0.5f, 0.5f);
    public Vector2 titleAnchoredPosition = Vector2.zero;
    public Vector2 titleSize = new Vector2(1600f, 180f);

    [Header("Subtitle")]
    public TextStyle subtitleStyle = new TextStyle
    {
        fontSize = 32,
        bold = false,
        color = new Color(0.68f, 0.68f, 0.68f, 1f),
        alignment = TextAnchor.MiddleCenter
    };

    public Vector2 subtitleAnchor = new Vector2(0.5f, 0.5f);
    public Vector2 subtitleAnchoredPosition = new Vector2(0f, -130f);
    public Vector2 subtitleSize = new Vector2(1400f, 120f);

    [Header("Timing")]
    public float fadeInDuration = 0.6f;
    public float displayDuration = 2.2f;
    public float fadeOutDuration = 0.6f;

    public bool useUnscaledTime = true;
}
