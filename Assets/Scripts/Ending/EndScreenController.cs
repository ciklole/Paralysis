using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the end-of-demo screen. The title and the text lines fly in from
/// alternating sides, then the back button fades in. Everything is authored in
/// End.unity; this component only animates the referenced objects and handles
/// the return to the main menu.
/// </summary>
public class EndScreenController : MonoBehaviour
{
    [SerializeField] private EndScreenView authoredView;

    [Header("Fly-in")]
    [Min(0f)]
    [Tooltip("Pause before the first word starts moving.")]
    [SerializeField] private float startDelay = 0.35f;

    [Min(0.01f)]
    [Tooltip("Seconds for one word or line to travel into place.")]
    [SerializeField] private float flyDuration = 0.9f;

    [Min(0f)]
    [Tooltip("Delay between consecutive lines.")]
    [SerializeField] private float lineStagger = 0.32f;

    [Min(0f)]
    [Tooltip("How far off-screen the words start, in UI pixels.")]
    [SerializeField] private float offscreenDistance = 1400f;

    [Range(0.5f, 1f)]
    [Tooltip("Scale the title starts at before settling to its authored size.")]
    [SerializeField] private float titleScaleFrom = 0.9f;

    [Header("Back Button")]
    [Min(0f)]
    [SerializeField] private float buttonDelay = 0.5f;

    [Min(0.01f)]
    [SerializeField] private float buttonFadeDuration = 0.6f;

    [Min(0f)]
    [Tooltip("How far below its authored spot the button starts, in pixels.")]
    [SerializeField] private float buttonRise = 40f;

    [Header("Music")]
    [Tooltip("Cue used for the ending. Leave empty to keep the current track.")]
    [SerializeField] private string endCueId = MusicCueIds.End;

    [Min(0f)]
    [SerializeField] private float musicFadeOut = 2.5f;

    [Tooltip(
        "When the ending cue has no clip assigned yet, fade the current " +
        "music out instead of letting the previous act theme continue.")]
    [SerializeField] private bool stopMusicWhenEndCueIsEmpty = true;

    [Header("Scene")]
    [Tooltip(
        "Optional override. Leave empty to use SceneFlow's main menu scene.")]
    [SerializeField] private string mainMenuScene = "";

    private EndScreenView view;
    private Coroutine introRoutine;
    private bool returning;

    public EndScreenView View => view;
    public bool AnimationComplete { get; private set; }
    public bool IsReturning => returning;
    public MenuButton BackButton => view != null ? view.backButton : null;

    private void Awake()
    {
        if (authoredView == null)
            authoredView = FindAnyObjectByType<EndScreenView>();

        view = authoredView != null
            ? authoredView
            : GetComponent<EndScreenView>();
    }

    private void Start()
    {
        // Gameplay keeps the pointer locked and hidden. The ending is a plain
        // UI screen, so hand the cursor back before the back button appears.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (view == null)
        {
            Debug.LogWarning(
                "EndScreenController: no EndScreenView found. The screen " +
                "will show but the return button has nothing to bind to.",
                this
            );

            return;
        }

        if (view.backButton != null)
        {
            view.backButton.Bind(view.menuAudio);
            view.backButton.Clicked += ReturnToMainMenu;
        }

        PrepareMusic();
        introRoutine = StartCoroutine(PlayIntro());
    }

    private void OnDestroy()
    {
        if (view != null && view.backButton != null)
            view.backButton.Clicked -= ReturnToMainMenu;
    }

    /// <summary>
    /// Leaves the ending and loads the main menu. Safe to call more than once;
    /// only the first request is used.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (returning)
            return;

        returning = true;

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        if (SceneFlow.GoToMainMenu())
            return;

        string scene = string.IsNullOrEmpty(mainMenuScene)
            ? SceneFlow.MainMenuScene
            : mainMenuScene;

        if (Application.CanStreamedLevelBeLoaded(scene))
        {
            SceneManager.LoadScene(scene);
            return;
        }

        returning = false;

        Debug.LogError(
            "EndScreenController: '" + scene + "' is not in Build Settings, " +
            "so the main menu cannot be opened.",
            this
        );
    }

    private void PrepareMusic()
    {
        MusicManager manager = MusicManager.Instance;

        if (manager == null)
            return;

        MusicDatabase database = manager.Database;
        MusicCue cue = database != null && !string.IsNullOrEmpty(endCueId)
            ? database.FindCue(endCueId)
            : null;

        if (cue != null && cue.clip != null)
        {
            MusicManager.PlayCue(endCueId);
            return;
        }

        if (stopMusicWhenEndCueIsEmpty)
            MusicManager.StopMusic(musicFadeOut);
    }

    private IEnumerator PlayIntro()
    {
        Text[] lines = view.lines ?? new Text[0];

        CanvasGroup[] lineGroups = new CanvasGroup[lines.Length];
        Vector2[] lineTargets = new Vector2[lines.Length];
        Vector2[] lineStarts = new Vector2[lines.Length];
        int lineCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            Text line = lines[i];

            if (line == null)
                continue;

            RectTransform rect = line.rectTransform;

            lineGroups[i] = EnsureGroup(line.gameObject);
            lineTargets[i] = rect.anchoredPosition;
            lineStarts[i] = lineTargets[i] + new Vector2(
                lineCount % 2 == 0 ? -offscreenDistance : offscreenDistance,
                0f
            );

            rect.anchoredPosition = lineStarts[i];
            lineGroups[i].alpha = 0f;
            lineCount++;
        }

        CanvasGroup titleGroup = view.titleText != null
            ? EnsureGroup(view.titleText.gameObject)
            : null;

        RectTransform titleRect = view.titleText != null
            ? view.titleText.rectTransform
            : null;

        Vector2 titleTarget = titleRect != null
            ? titleRect.anchoredPosition
            : Vector2.zero;

        Vector2 titleStart = titleTarget + new Vector2(
            -offscreenDistance,
            0f
        );

        Vector3 titleScaleFromVector =
            Vector3.one * Mathf.Clamp01(titleScaleFrom);

        if (titleRect != null)
            titleRect.anchoredPosition = titleStart;

        if (titleGroup != null)
            titleGroup.alpha = 0f;

        CanvasGroup buttonGroup = view.backButton != null
            ? EnsureGroup(view.backButton.gameObject)
            : null;

        RectTransform buttonRect = view.backButton != null
            ? view.backButton.transform as RectTransform
            : null;

        Vector2 buttonTarget = buttonRect != null
            ? buttonRect.anchoredPosition
            : Vector2.zero;

        Vector2 buttonStart = buttonTarget + Vector2.down * buttonRise;

        if (buttonRect != null)
            buttonRect.anchoredPosition = buttonStart;

        if (buttonGroup != null)
            buttonGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < startDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;

        float linesDelay = 0.2f;
        float lastLineDelay = linesDelay +
                              Mathf.Max(0, lineCount - 1) * lineStagger;

        float total = Mathf.Max(flyDuration, lastLineDelay + flyDuration);

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;

            Animate(
                titleGroup,
                titleRect,
                titleStart,
                titleTarget,
                titleScaleFromVector,
                Vector3.one,
                elapsed,
                0f,
                flyDuration
            );

            int visible = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null)
                    continue;

                Animate(
                    lineGroups[i],
                    lines[i].rectTransform,
                    lineStarts[i],
                    lineTargets[i],
                    Vector3.one,
                    Vector3.one,
                    elapsed,
                    linesDelay + visible * lineStagger,
                    flyDuration
                );

                visible++;
            }

            yield return null;
        }

        Finish(titleGroup, titleRect, titleTarget, Vector3.one);

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null)
                continue;

            Finish(
                lineGroups[i],
                lines[i].rectTransform,
                lineTargets[i],
                Vector3.one
            );
        }

        elapsed = 0f;

        while (elapsed < buttonDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = EaseOutCubic(
                Mathf.Clamp01(elapsed / buttonFadeDuration)
            );

            if (buttonGroup != null)
                buttonGroup.alpha = t;

            if (buttonRect != null)
            {
                buttonRect.anchoredPosition = Vector2.LerpUnclamped(
                    buttonStart,
                    buttonTarget,
                    t
                );
            }

            yield return null;
        }

        if (buttonGroup != null)
            buttonGroup.alpha = 1f;

        if (buttonRect != null)
            buttonRect.anchoredPosition = buttonTarget;

        AnimationComplete = true;
        introRoutine = null;
    }

    private static void Animate(
        CanvasGroup group,
        RectTransform rect,
        Vector2 from,
        Vector2 to,
        Vector3 scaleFrom,
        Vector3 scaleTo,
        float elapsed,
        float delay,
        float duration)
    {
        float t = Mathf.Clamp01(
            (elapsed - delay) / Mathf.Max(0.01f, duration)
        );

        if (t <= 0f)
            return;

        float eased = EaseOutCubic(t);

        if (group != null)
            group.alpha = eased;

        if (rect != null)
        {
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            rect.localScale = Vector3.LerpUnclamped(
                scaleFrom,
                scaleTo,
                eased
            );
        }
    }

    private static void Finish(
        CanvasGroup group,
        RectTransform rect,
        Vector2 target,
        Vector3 scale)
    {
        if (group != null)
            group.alpha = 1f;

        if (rect == null)
            return;

        rect.anchoredPosition = target;
        rect.localScale = scale;
    }

    private static CanvasGroup EnsureGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - Mathf.Clamp01(t);
        return 1f - inverse * inverse * inverse;
    }
}
