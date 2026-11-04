using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent loading-screen and chapter-introduction presentation. The
/// black overlay, loading label and intro texts are created once and reused
/// for every transition, following the visual language of Teleport.cs.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private SceneFlowConfig config;
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform blackRoot;
    private CanvasGroup blackGroup;
    private Text loadingText;
    private LoadingIndicator loadingIndicator;
    private ChapterIntroScreen introScreen;
    private AudioSource audioSource;

    public bool IsBusy { get; private set; }

    public static SceneTransitionManager Ensure()
    {
        if (Instance != null)
            return Instance;

        SceneTransitionManager existing =
            FindAnyObjectByType<SceneTransitionManager>();

        if (existing != null)
        {
            Instance = existing;
            existing.Initialize();
            return existing;
        }

        GameObject go = new GameObject("SceneTransitionManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SceneTransitionManager>();

        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (config == null)
            config = Resources.Load<SceneFlowConfig>(
                "Paralysis/SceneFlowConfig"
            );

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SceneFlowConfig>();
            config.name = "RuntimeSceneFlowConfig";
        }

        if (canvas == null)
            BuildUI();
    }

    private void BuildUI()
    {
        if (TryCreateAuthoredUI())
            return;

        canvas = UIHelper.CreateCanvas("SceneTransitionCanvas", 600, true);
        canvasRect = canvas.GetComponent<RectTransform>();

        blackRoot = UIHelper.CreateStretchRect(
            "TransitionBlack",
            canvas.transform,
            Vector2.zero,
            Vector2.zero
        );

        Image blackImage = UIHelper.AddPanel(blackRoot, Color.black);
        blackImage.raycastTarget = true;

        blackGroup = blackRoot.gameObject.AddComponent<CanvasGroup>();
        blackGroup.alpha = 0f;
        blackGroup.blocksRaycasts = false;

        RectTransform loadingRect = UIHelper.CreateRect(
            "LoadingText",
            blackRoot,
            config.loadingAnchor,
            config.loadingAnchor,
            config.loadingAnchoredPosition,
            config.loadingSize
        );

        loadingText = UIHelper.AddText(
            loadingRect,
            config.loadingStyle,
            config.loadingText + "..."
        );

        loadingText.gameObject.AddComponent<GameFontIgnore>();
        loadingText.raycastTarget = false;

        loadingIndicator =
            loadingText.gameObject.AddComponent<LoadingIndicator>();

        loadingIndicator.Configure(
            loadingText,
            config.loadingText,
            config.animateDots,
            config.dotStepInterval
        );

        GameObject introObject = new GameObject("ChapterIntroScreen");
        introObject.transform.SetParent(canvas.transform, false);
        introScreen = introObject.AddComponent<ChapterIntroScreen>();
        introScreen.Build(canvas.transform);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        blackRoot.gameObject.SetActive(false);
    }

    /// <summary>
    /// Instantiates the editable loading/transition Canvas. The same fade,
    /// loading-dot and chapter-intro coroutines drive the authored objects.
    /// </summary>
    private bool TryCreateAuthoredUI()
    {
        if (config == null || config.transitionPrefab == null)
            return false;

        UIHelper.EnsureEventSystem();

        GameObject instance = Instantiate(
            config.transitionPrefab,
            transform,
            false
        );

        instance.name = "SceneTransitionCanvas";

        TransitionView authored =
            instance.GetComponentInChildren<TransitionView>(true);

        if (authored == null ||
            authored.blackRoot == null ||
            authored.blackGroup == null ||
            authored.loadingText == null)
        {
            Debug.LogWarning(
                "SceneTransitionManager: the authored transition prefab is " +
                "missing its black overlay or loading text. Falling back to " +
                "the runtime loading screen.",
                authored
            );

            Destroy(instance);
            return false;
        }

        canvas = authored.canvas != null
            ? authored.canvas
            : instance.GetComponent<Canvas>();

        if (canvas == null)
        {
            Destroy(instance);
            return false;
        }

        canvasRect = authored.canvasRect != null
            ? authored.canvasRect
            : canvas.GetComponent<RectTransform>();

        blackRoot = authored.blackRoot;
        blackGroup = authored.blackGroup;
        loadingText = authored.loadingText;

        loadingIndicator = authored.loadingIndicator != null
            ? authored.loadingIndicator
            : loadingText.GetComponent<LoadingIndicator>();

        if (loadingIndicator == null)
        {
            loadingIndicator =
                loadingText.gameObject.AddComponent<LoadingIndicator>();
        }

        string baseText =
            authored.useAuthoredLoadingText &&
            !string.IsNullOrEmpty(loadingText.text)
                ? loadingText.text
                : config.loadingText;

        loadingIndicator.Configure(
            loadingText,
            baseText,
            config.animateDots,
            config.dotStepInterval
        );

        if (loadingText.GetComponent<GameFontIgnore>() == null)
            loadingText.gameObject.AddComponent<GameFontIgnore>();

        introScreen = authored.introScreen;

        if (introScreen == null)
        {
            introScreen =
                instance.GetComponentInChildren<ChapterIntroScreen>(true);
        }

        if (introScreen == null)
        {
            GameObject introObject = new GameObject("ChapterIntroScreen");
            introObject.transform.SetParent(canvas.transform, false);
            introScreen = introObject.AddComponent<ChapterIntroScreen>();
        }

        introScreen.Build(canvas.transform);

        audioSource = authored.audioSource != null
            ? authored.audioSource
            : gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        blackRoot.gameObject.SetActive(false);
        return true;
    }

    public void StartTransition(
        TransitionRequest request,
        Action<bool, string> complete)
    {
        if (IsBusy || request == null)
        {
            complete?.Invoke(false, "A transition is already running.");
            return;
        }

        IsBusy = true;
        StartCoroutine(RunTransition(request, complete));
    }

    private IEnumerator RunTransition(
        TransitionRequest request,
        Action<bool, string> complete)
    {
        bool success = false;
        string error = null;
        float previousTimeScale = Time.timeScale;
        CursorLockMode previousLock = Cursor.lockState;
        bool previousVisible = Cursor.visible;

        try
        {
            if (config == null)
                Initialize();

            if (!Application.CanStreamedLevelBeLoaded(request.sceneName))
            {
                error =
                    "Scene '" + request.sceneName +
                    "' is not in Build Settings.";
                Debug.LogError("SceneTransitionManager: " + error);
                yield break;
            }

            blackRoot.gameObject.SetActive(true);
            blackGroup.alpha = 0f;
            blackGroup.blocksRaycasts = true;
            loadingText.gameObject.SetActive(true);
            loadingIndicator.ResetAnimation();
            introScreen.HideImmediate();

            Time.timeScale = 0f;

            if (config.hideCursorDuringTransitions)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            UIState.PushModal(this);
            PlayerControl.Lock(this);

            if (config.transitionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(
                    config.transitionSound,
                    Mathf.Clamp01(config.transitionVolume)
                );
            }

            yield return Fade(
                blackGroup,
                0f,
                1f,
                config.fadeInDuration
            );

            AsyncOperation operation = SceneManager.LoadSceneAsync(
                request.sceneName,
                LoadSceneMode.Single
            );

            if (operation == null)
            {
                error =
                    "Unity could not start loading '" +
                    request.sceneName + "'.";
                Debug.LogError("SceneTransitionManager: " + error);
                yield break;
            }

            operation.allowSceneActivation = false;

            float loadStart = Time.realtimeSinceStartup;

            while (operation.progress < 0.9f)
            {
                if (Time.realtimeSinceStartup - loadStart >
                    config.loadingTimeout)
                {
                    error =
                        "Timed out while loading '" +
                        request.sceneName + "'.";
                    Debug.LogError("SceneTransitionManager: " + error);
                    yield break;
                }

                yield return null;
            }

            while (Time.realtimeSinceStartup - loadStart <
                   config.minimumLoadingDuration)
            {
                yield return null;
            }

            if (request.showChapterIntro && request.chapter != null)
            {
                loadingText.gameObject.SetActive(false);

                // ChapterDatabase owns the global intro look. SceneFlowConfig
                // is only a fallback for projects without a database.
                ChapterDatabase database = ChapterDatabase.Load();
                ChapterIntroStyle globalStyle =
                    database != null && database.defaultIntroStyle != null
                        ? database.defaultIntroStyle
                        : config.defaultIntroStyle;

                ChapterIntroStyle style = request.chapter
                    .useGlobalIntroStyle
                    ? globalStyle
                    : request.chapter.introStyle;

                if (style == null)
                    style = globalStyle;

                yield return introScreen.Play(request.chapter, style);
            }

            // The scene is fully prepared but has not run its Awake/Start yet.
            // Activating now means story intros cannot start behind the black
            // chapter screen.
            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            // Let the new scene's Awake/Start and one-shot placement run.
            yield return null;

            SceneFlow.NotifySceneActivated(request.sceneName);

            yield return Fade(
                blackGroup,
                1f,
                0f,
                config.fadeOutDuration
            );

            success = true;
        }
        finally
        {
            blackRoot.gameObject.SetActive(false);
            blackGroup.blocksRaycasts = false;
            introScreen.HideImmediate();

            UIState.PopModal(this);
            PlayerControl.Unlock(this);

            Time.timeScale = success ? 1f : previousTimeScale;

            if (!success)
            {
                Cursor.lockState = previousLock;
                Cursor.visible = previousVisible;
            }
            else if (request != null &&
                     request.sceneName == SceneFlow.MainMenuScene)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (config != null && config.hideCursorDuringTransitions)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            IsBusy = false;
            complete?.Invoke(success, error);
        }
    }

    private static IEnumerator Fade(
        CanvasGroup group,
        float from,
        float to,
        float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0.0001f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(
                from,
                to,
                Mathf.SmoothStep(0f, 1f, t)
            );

            yield return null;
        }

        group.alpha = to;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
