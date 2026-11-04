using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The black chapter-introduction screen. It is built once and reused; each
/// chapter only applies its own text, style, positions and timings.
/// </summary>
public class ChapterIntroScreen : MonoBehaviour
{
    [SerializeField] private ChapterIntroView view;

    private RectTransform root;
    private CanvasGroup group;
    private Text numberText;
    private Text titleText;
    private Text subtitleText;

    public bool IsPlaying { get; private set; }

    public void SetView(ChapterIntroView authoredView)
    {
        view = authoredView;
    }

    public void Build(Transform parent)
    {
        if (root != null)
            return;

        if (view != null)
        {
            BindAuthored();
            return;
        }

        root = UIHelper.CreateStretchRect(
            "ChapterIntro",
            parent,
            Vector2.zero,
            Vector2.zero
        );

        Image background = UIHelper.AddPanel(root, Color.black);
        background.raycastTarget = true;

        group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;

        numberText = CreateText("ChapterNumber");
        titleText = CreateText("ChapterTitle");
        subtitleText = CreateText("ChapterSubtitle");

        root.gameObject.SetActive(false);
    }

    private void BindAuthored()
    {
        root = view.root != null
            ? view.root
            : transform as RectTransform;

        if (root == null)
            return;

        group = view.group != null
            ? view.group
            : root.GetComponent<CanvasGroup>();

        if (group == null)
            group = root.gameObject.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        group.blocksRaycasts = true;

        numberText = view.numberText;
        titleText = view.titleText;
        subtitleText = view.subtitleText;

        root.gameObject.SetActive(false);
    }

    private Text CreateText(string name)
    {
        RectTransform rect = UIHelper.CreateRect(
            name,
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1400f, 120f)
        );

        Text text = UIHelper.AddText(rect, new TextStyle(), string.Empty);
        text.gameObject.AddComponent<GameFontIgnore>();
        text.raycastTarget = false;

        return text;
    }

    public void Apply(
        ChapterDefinition chapter,
        ChapterIntroStyle style)
    {
        if (chapter == null || style == null)
            return;

        if (view != null && !view.applyChapterStyles)
        {
            ApplyContent(
                numberText,
                string.Format(style.numberFormat, chapter.chapterNumber)
            );

            ApplyContent(titleText, chapter.chapterTitle);
            ApplyContent(subtitleText, chapter.subtitle);

            if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(
                    !string.IsNullOrEmpty(chapter.subtitle)
                );
            }

            return;
        }

        ApplyText(
            numberText,
            string.Format(style.numberFormat, chapter.chapterNumber),
            style.numberStyle,
            style.numberAnchor,
            style.numberAnchoredPosition,
            style.numberSize
        );

        ApplyText(
            titleText,
            chapter.chapterTitle,
            style.titleStyle,
            style.titleAnchor,
            style.titleAnchoredPosition,
            style.titleSize
        );

        ApplyText(
            subtitleText,
            chapter.subtitle,
            style.subtitleStyle,
            style.subtitleAnchor,
            style.subtitleAnchoredPosition,
            style.subtitleSize
        );

        subtitleText.gameObject.SetActive(
            !string.IsNullOrEmpty(chapter.subtitle)
        );
    }

    private static void ApplyContent(Text text, string content)
    {
        if (text != null)
            text.text = content;
    }

    private static void ApplyText(
        Text text,
        string content,
        TextStyle style,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        style.Apply(text);
        text.text = content;
    }

    public IEnumerator Play(
        ChapterDefinition chapter,
        ChapterIntroStyle style)
    {
        if (root == null || chapter == null || style == null)
            yield break;

        IsPlaying = true;
        Apply(chapter, style);

        root.gameObject.SetActive(true);
        group.alpha = 0f;

        // The loading screen freezes Time.timeScale while the scene finishes
        // preparing. Always use unscaled time when frozen so an inspector
        // setting cannot make the intro hang forever.
        bool unscaled = style.useUnscaledTime || Time.timeScale <= 0.0001f;

        float fadeIn = Mathf.Max(0f, style.fadeInDuration);
        float display = Mathf.Max(0f, style.displayDuration);
        float fadeOut = Mathf.Max(0f, style.fadeOutDuration);

        yield return Fade(0f, 1f, fadeIn, unscaled);

        if (display > 0f)
        {
            float elapsed = 0f;

            while (elapsed < display)
            {
                elapsed += unscaled
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                yield return null;
            }
        }

        yield return Fade(1f, 0f, fadeOut, unscaled);

        group.alpha = 0f;
        root.gameObject.SetActive(false);
        IsPlaying = false;
    }

    private IEnumerator Fade(
        float from,
        float to,
        float duration,
        bool unscaled)
    {
        if (duration <= 0.0001f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += unscaled
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        group.alpha = to;
    }

    public void HideImmediate()
    {
        StopAllCoroutines();
        IsPlaying = false;

        if (group != null)
            group.alpha = 0f;

        if (root != null)
            root.gameObject.SetActive(false);
    }
}
