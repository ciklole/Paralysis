using UnityEngine;

/// <summary>
/// Appearance and motion for one text-only menu button. The same style can be
/// shared by every button in a menu, so changing it in the Inspector updates
/// the whole group at once.
/// </summary>
[System.Serializable]
public class MenuButtonStyle
{
    [Header("Motion")]
    [Tooltip("How far the label moves up while hovered, in UI pixels.")]
    public float hoverOffset = 10f;

    [Tooltip("Seconds for the hover transition in either direction.")]
    public float duration = 0.16f;

    [Tooltip("Easing curve used for the hover transition.")]
    public AnimationCurve easing =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Normal Appearance")]
    public int normalFontSize = 36;
    public Color normalColor = new Color(0.72f, 0.72f, 0.72f, 1f);
    public bool normalBold = false;
    public Font normalFont;

    [Header("Hover Appearance")]
    [Tooltip("Set to 0 to keep the normal font size.")]
    public int hoverFontSize = 40;
    public Color hoverColor = Color.white;
    public bool hoverBold = true;
    public Font hoverFont;

    [Header("Other States")]
    public Color selectedColor = new Color(1f, 0.84f, 0.45f, 1f);
    public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public int ResolveHoverSize()
    {
        return hoverFontSize > 0 ? hoverFontSize : normalFontSize;
    }
}
