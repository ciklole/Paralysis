using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Text-only clickable menu entry. The invisible Image on this object is the
/// hitbox; the label and optional description are separate Text children, so
/// the text can move on hover without changing where the player has to click.
/// </summary>
public class MenuButton :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField] private Text label;
    [SerializeField] private Text secondaryLabel;
    [SerializeField] private Image hitbox;
    [SerializeField] private MenuButtonStyle style = new MenuButtonStyle();
    [SerializeField] private MenuAudio audioPlayer;
    [SerializeField] private bool interactable = true;
    [SerializeField] private bool selected;

    [Header("Authored Label")]
    [SerializeField]
    [Tooltip(
        "Let the shared style override the font size set on this button's " +
        "label. Leave this off to keep the size you set in the Canvas.")]
    private bool useStyleFontSize;

    [SerializeField] private UnityEvent onClick = new UnityEvent();

    private Vector2 labelBasePosition;
    private float progress;
    private bool hovering;
    private bool animating;
    private Font baseFont;
    private Color normalColor;
    private int normalSize;
    private int authoredFontSize;
    private bool authoredFontSizeCaptured;
    private bool useAuthoredFontSize;
    private bool visualInitialized;

    public event Action Clicked;

    public bool Interactable => interactable;
    public bool Selected => selected;
    public Text Label => label;
    public Text SecondaryLabel => secondaryLabel;
    public Image Hitbox => hitbox;
    public MenuButtonStyle Style => style;
    public int AuthoredFontSize => authoredFontSize;

    private void Awake()
    {
        CaptureAuthoredFontSize();
    }

    public void Configure(
        Text labelText,
        Text secondaryText,
        Image hitboxImage,
        MenuButtonStyle buttonStyle,
        MenuAudio audio)
    {
        label = labelText;
        secondaryLabel = secondaryText;
        hitbox = hitboxImage;
        style = buttonStyle ?? new MenuButtonStyle();
        audioPlayer = audio;

        // Buttons created by the runtime builders always follow the style.
        useAuthoredFontSize = false;
        authoredFontSizeCaptured = true;
        visualInitialized = true;

        if (label != null)
        {
            baseFont = label.font;
            labelBasePosition = label.rectTransform.anchoredPosition;
        }

        if (hitbox != null)
            hitbox.raycastTarget = true;

        progress = 0f;
        hovering = false;
        animating = false;
        ApplyVisual(0f);
    }

    /// <summary>
    /// Wires an already-authored button. Unlike Configure, this keeps the
    /// serialized MenuButtonStyle and only supplies the shared audio player.
    /// </summary>
    public void Bind(MenuAudio audio)
    {
        audioPlayer = audio;

        // Authored buttons keep the font size set on their label unless the
        // shared style is explicitly allowed to override it.
        CaptureAuthoredFontSize();
        useAuthoredFontSize = !useStyleFontSize && authoredFontSize > 0;
        visualInitialized = true;

        if (label != null)
        {
            baseFont = label.font;
            labelBasePosition = label.rectTransform.anchoredPosition;
        }

        if (hitbox != null)
            hitbox.raycastTarget = interactable;

        progress = 0f;
        hovering = false;
        animating = false;
        ApplyVisual(0f);
    }

    private void CaptureAuthoredFontSize()
    {
        if (authoredFontSizeCaptured || label == null)
            return;

        authoredFontSize = label.fontSize;
        authoredFontSizeCaptured = true;
    }

    public void SetLabel(string text)
    {
        if (label != null)
            label.text = text;
    }

    public void SetSecondaryLabel(string text)
    {
        if (secondaryLabel == null)
            return;

        secondaryLabel.text = text;
        secondaryLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (!value)
        {
            hovering = false;
            animating = false;
            progress = 0f;
            ApplyVisual(0f);
        }

        if (hitbox != null)
            hitbox.raycastTarget = value;
    }

    public void SetSelected(bool value)
    {
        selected = value;
        ApplyVisual(progress);
    }

    public void RefreshBasePosition()
    {
        if (label != null)
            labelBasePosition = label.rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable)
            return;

        hovering = true;
        animating = true;

        if (audioPlayer != null)
            audioPlayer.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        animating = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null &&
            eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Activate();
    }

    /// <summary>
    /// Runs the same action as a left click. Used by keyboard/gamepad input
    /// adapters and by the automated menu tests.
    /// </summary>
    public void Activate()
    {
        if (!interactable)
            return;

        if (audioPlayer != null)
            audioPlayer.PlayClick();

        onClick.Invoke();
        Clicked?.Invoke();
    }

    private void Update()
    {
        if (!animating)
            return;

        float target = hovering && interactable ? 1f : 0f;
        progress = UiAnimation.MoveTowards(
            progress,
            target,
            style.duration,
            Time.unscaledDeltaTime
        );

        ApplyVisual(progress);

        if (Mathf.Approximately(progress, target))
        {
            progress = target;
            animating = false;
        }
    }

    private void ApplyVisual(float t)
    {
        if (label == null)
            return;

        float eased = UiAnimation.Evaluate(style.easing, t);

        label.rectTransform.anchoredPosition =
            labelBasePosition + Vector2.up * style.hoverOffset * eased;

        normalColor = selected ? style.selectedColor : style.normalColor;
        normalSize = useAuthoredFontSize && authoredFontSize > 0
            ? authoredFontSize
            : style.normalFontSize;

        int hoverSize = ResolveHoverSize();

        label.fontSize = Mathf.RoundToInt(
            Mathf.Lerp(normalSize, hoverSize, eased)
        );

        label.color = Color.Lerp(normalColor, style.hoverColor, eased);

        if (!interactable)
            label.color = style.disabledColor;

        bool bold = eased > 0.5f ? style.hoverBold : style.normalBold;

        if (selected && eased <= 0.5f)
            bold = style.normalBold;

        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

        Font targetFont = null;

        if (eased > 0.01f && style.hoverFont != null)
            targetFont = style.hoverFont;
        else if (style.normalFont != null)
            targetFont = style.normalFont;
        else
            targetFont = baseFont;

        if (targetFont != null)
            label.font = targetFont;

        if (secondaryLabel != null)
        {
            Color secondary = secondaryLabel.color;
            secondary.a = interactable ? 0.65f : 0.25f;
            secondaryLabel.color = secondary;
        }
    }

    private int ResolveHoverSize()
    {
        if (!useAuthoredFontSize || authoredFontSize <= 0)
            return style.ResolveHoverSize();

        if (style.hoverFontSize <= 0)
            return authoredFontSize;

        if (style.normalFontSize <= 0)
            return style.hoverFontSize;

        // Scale the style's hover growth to the authored size so a larger
        // label still grows by the same proportion.
        float scale = (float)style.hoverFontSize / style.normalFontSize;

        return Mathf.Max(1, Mathf.RoundToInt(authoredFontSize * scale));
    }

    private void OnDisable()
    {
        // Before Bind/Configure the label still holds the size authored in the
        // Canvas. Applying the style here would overwrite it.
        if (!visualInitialized)
            return;

        hovering = false;
        animating = false;
        progress = 0f;
        ApplyVisual(0f);
    }
}
