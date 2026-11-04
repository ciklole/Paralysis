using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One persistent pause menu for every gameplay scene. ESC slides in a black
/// panel from the left while the live game stays visible, darkened, on the
/// right. Time is frozen with Time.timeScale, but all menu motion uses
/// unscaled time so the animation still plays.
/// </summary>
public class PauseManager : MonoBehaviour
{
    private enum State
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    private const int CanvasSortingOrder = 450;
    private const string ConfigResourcePath = "Paralysis/PauseMenuConfig";

    public static PauseManager Instance { get; private set; }

    [SerializeField] private PauseMenuConfig config;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform leftPanel;
    private Image leftImage;
    private Image rightOverlay;
    private RectTransform contentGroup;
    private CanvasGroup contentCanvasGroup;
    private RectTransform contentShift;
    private RectTransform buttonsRoot;
    private Text titleText;
    private SettingsPanel settingsPanel;
    private MenuAudio menuAudio;
    private MenuButton continueButton;
    private MenuButton settingsButton;
    private MenuButton replayButton;
    private MenuButton mainMenuButton;
    private PauseMenuView view;
    private Color authoredLeftColor = Color.black;
    private Color authoredRightColor = new Color(0f, 0f, 0f, 0.62f);

    private State state = State.Closed;
    private float progress;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLock = CursorLockMode.None;
    private bool previousCursorVisible = true;
    private bool paused;
    private bool settingsVisible;

    public bool IsOpen => paused;
    public bool IsSettingsOpen => settingsVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Ensure();
    }

    public static PauseManager Ensure()
    {
        if (Instance != null)
            return Instance;

        PauseManager existing = FindAnyObjectByType<PauseManager>();

        if (existing != null)
        {
            Instance = existing;
            existing.LoadConfig();
            return existing;
        }

        GameObject go = new GameObject("PauseManager");
        DontDestroyOnLoad(go);

        return go.AddComponent<PauseManager>();
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
        LoadConfig();
    }

    private void LoadConfig()
    {
        if (config != null)
            return;

        config = Resources.Load<PauseMenuConfig>(ConfigResourcePath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PauseMenuConfig>();
            config.name = "RuntimePauseMenuConfig";
        }
    }

    private void Update()
    {
        if (config == null)
            LoadConfig();

        if (SceneFlow.IsTransitioning)
            return;

        if (IsMainMenuActive())
        {
            if (paused)
                HardResetForMenu();

            return;
        }

        if (config != null && Input.GetKeyDown(config.pauseKey))
        {
            if (paused)
            {
                // ESC never resumes from inside the settings screen.
                if (settingsVisible)
                    HideSettings();
                else
                    BeginResume();
            }
            else if (state == State.Closed)
            {
                bool modalOpen = UIState.BlocksGameplay;

                if (!modalOpen ||
                    (config != null && config.allowDuringModals))
                {
                    Open();
                }
            }
        }

        if (paused)
            UpdateAnimation();
    }

    private static bool IsMainMenuActive()
    {
        Scene scene = SceneManager.GetActiveScene();

        return scene.IsValid() &&
               string.Equals(
                   scene.name,
                   SceneFlow.MainMenuScene,
                   StringComparison.OrdinalIgnoreCase);
    }

    public bool Open()
    {
        if (paused || state != State.Closed)
            return false;

        if (IsMainMenuActive() || SceneFlow.IsTransitioning)
            return false;

        EnsureUI();

        if (canvas == null)
            return false;

        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        paused = true;
        settingsVisible = false;
        state = State.Opening;
        progress = 0f;

        if (settingsPanel != null)
            settingsPanel.Hide();

        if (contentGroup != null)
            contentGroup.gameObject.SetActive(true);

        canvas.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        ApplyAnimation(0f);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UIState.PushModal(this);
        PlayerControl.Lock(this);

        return true;
    }

    public void Resume()
    {
        if (!paused || state == State.Closing)
            return;

        BeginResume();
    }

    public void OpenSettings()
    {
        if (!paused || settingsVisible)
            return;

        EnsureUI();

        if (settingsPanel == null)
            return;

        settingsVisible = true;

        if (contentGroup != null)
            contentGroup.gameObject.SetActive(false);

        settingsPanel.Show();
    }

    public void HideSettings()
    {
        if (!settingsVisible)
            return;

        settingsVisible = false;

        if (settingsPanel != null)
            settingsPanel.Hide();

        if (contentGroup != null)
        {
            contentGroup.gameObject.SetActive(true);
            ApplyAnimation(1f);
        }
    }

    private void BeginResume()
    {
        if (settingsVisible)
            HideSettings();

        state = State.Closing;
        progress = 0f;
    }

    private void FinishResume()
    {
        state = State.Closed;
        paused = false;
        settingsVisible = false;

        if (canvas != null)
            canvas.gameObject.SetActive(false);

        UIState.PopModal(this);
        PlayerControl.Unlock(this);

        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void UpdateAnimation()
    {
        if (state != State.Opening && state != State.Closing)
            return;

        float duration = state == State.Opening
            ? config.openDuration
            : config.closeDuration;

        if (duration <= 0.0001f)
        {
            progress = 1f;
        }
        else
        {
            progress += Time.unscaledDeltaTime / duration;
        }

        float clamped = Mathf.Clamp01(progress);
        float eased = UiAnimation.Evaluate(config.easing, clamped);

        ApplyAnimation(
            state == State.Opening ? eased : 1f - eased
        );

        if (clamped < 1f)
            return;

        if (state == State.Opening)
        {
            state = State.Open;
            ApplyAnimation(1f);
        }
        else
        {
            FinishResume();
        }
    }

    private void ApplyAnimation(float amount)
    {
        if (canvasRect == null || leftPanel == null)
            return;

        float fraction = config.leftWidthFraction;

        if (view != null)
        {
            float authoredFraction =
                leftPanel.anchorMax.x - leftPanel.anchorMin.x;

            if (authoredFraction > 0.001f)
                fraction = authoredFraction;
        }

        float panelWidth = Mathf.Max(1f, canvasRect.rect.width) * fraction;

        // Stretch anchors: shifting the whole rect left by its width moves the
        // right edge from the middle of the screen to the left screen edge.
        leftPanel.anchoredPosition = new Vector2(
            -panelWidth * (1f - amount),
            0f
        );

        if (rightOverlay != null)
        {
            Color dark = view != null && view.useAuthoredColors
                ? authoredRightColor
                : config.rightDarkening;
            dark.a *= amount;
            rightOverlay.color = dark;
        }

        if (leftImage != null)
        {
            Color left = view != null && view.useAuthoredColors
                ? authoredLeftColor
                : config.leftPanelColor;
            left.a *= amount;
            leftImage.color = left;
        }

        if (contentCanvasGroup != null)
            contentCanvasGroup.alpha = amount;

        if (contentShift != null)
        {
            contentShift.anchoredPosition = new Vector2(
                -config.contentSlide * (1f - amount),
                0f
            );
        }
    }

    private void EnsureUI()
    {
        if (canvas != null)
            return;

        if (config == null)
            LoadConfig();

        GameSettings.EnsureInitialized();

        if (TryCreateAuthoredUI())
        {
            canvas.gameObject.SetActive(false);
            return;
        }

        canvas = UIHelper.CreateCanvas(
            "PauseCanvas",
            CanvasSortingOrder,
            true
        );

        canvasRect = canvas.GetComponent<RectTransform>();

        leftPanel = UIHelper.CreateStretchRect(
            "PauseLeftPanel",
            canvas.transform,
            Vector2.zero,
            Vector2.zero
        );

        leftPanel.anchorMin = new Vector2(0f, 0f);
        leftPanel.anchorMax = new Vector2(config.leftWidthFraction, 1f);
        leftPanel.offsetMin = Vector2.zero;
        leftPanel.offsetMax = Vector2.zero;

        leftImage = UIHelper.AddPanel(
            leftPanel,
            config.leftPanelColor
        );

        leftImage.raycastTarget = true;

        RectTransform rightPanel = UIHelper.CreateStretchRect(
            "PauseRightDarkening",
            canvas.transform,
            Vector2.zero,
            Vector2.zero
        );

        rightPanel.anchorMin = new Vector2(config.leftWidthFraction, 0f);
        rightPanel.anchorMax = new Vector2(1f, 1f);
        rightPanel.offsetMin = Vector2.zero;
        rightPanel.offsetMax = Vector2.zero;

        rightOverlay = UIHelper.AddPanel(
            rightPanel,
            config.rightDarkening
        );

        rightOverlay.raycastTarget = true;

        contentGroup = UIHelper.CreateStretchRect(
            "PauseContent",
            leftPanel,
            Vector2.zero,
            Vector2.zero
        );

        contentCanvasGroup =
            contentGroup.gameObject.AddComponent<CanvasGroup>();

        contentShift = UIHelper.CreateStretchRect(
            "PauseContentShift",
            contentGroup,
            Vector2.zero,
            Vector2.zero
        );

        if (config.showTitle)
        {
            RectTransform titleRect = UIHelper.CreateRect(
                "PauseTitle",
                contentShift,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(
                    config.titleOffset.x,
                    -config.titleOffset.y
                ),
                config.titleSize
            );

            titleText = UIHelper.AddText(
                titleRect,
                config.titleStyle,
                config.titleText
            );

            titleText.raycastTarget = false;
        }

        buttonsRoot = UIHelper.CreateStretchRect(
            "PauseButtons",
            contentShift,
            Vector2.zero,
            Vector2.zero
        );

        menuAudio = gameObject.AddComponent<MenuAudio>();
        menuAudio.Configure(config.audio);

        continueButton = CreateButton(
            config.continueLabel,
            0,
            BeginResume
        );

        settingsButton = CreateButton(
            config.settingsLabel,
            1,
            OpenSettings
        );

        replayButton = CreateButton(
            config.replayLabel,
            2,
            ReplayChapter
        );

        mainMenuButton = CreateButton(
            config.mainMenuLabel,
            3,
            ReturnToMainMenu
        );

        CreateSettingsPanel(leftPanel);

        canvas.gameObject.SetActive(false);
    }

    private void CreateSettingsPanel(Transform parent)
    {
        GameObject settingsHost = new GameObject("PauseSettings");
        settingsHost.transform.SetParent(parent, false);

        RectTransform settingsRect =
            settingsHost.AddComponent<RectTransform>();

        settingsRect.anchorMin = Vector2.zero;
        settingsRect.anchorMax = Vector2.one;
        settingsRect.offsetMin = Vector2.zero;
        settingsRect.offsetMax = Vector2.zero;

        settingsPanel = settingsHost.AddComponent<SettingsPanel>();

        settingsPanel.Build(
            settingsRect,
            config.settingsPanel,
            GameSettings.Config,
            menuAudio,
            HideSettings
        );

        settingsPanel.Hide();
    }

    /// <summary>
    /// Instantiates the editable PauseMenu prefab and binds the same animation
    /// fields the runtime builder would have created.
    /// </summary>
    private bool TryCreateAuthoredUI()
    {
        if (config == null || config.canvasPrefab == null)
            return false;

        UIHelper.EnsureEventSystem();

        GameObject instance = Instantiate(
            config.canvasPrefab,
            transform,
            false
        );

        instance.name = "PauseCanvas";

        PauseMenuView authored =
            instance.GetComponentInChildren<PauseMenuView>(true);

        if (authored == null)
        {
            Destroy(instance);
            return false;
        }

        if (authored.canvas == null)
            authored.canvas = instance.GetComponent<Canvas>();

        if (authored.canvas == null ||
            authored.leftPanel == null ||
            authored.leftImage == null ||
            authored.contentGroup == null ||
            authored.contentCanvasGroup == null ||
            authored.contentShift == null ||
            authored.buttonsRoot == null)
        {
            Debug.LogWarning(
                "PauseManager: the authored PauseMenu prefab is missing " +
                "its split-screen references. Falling back to the runtime " +
                "pause menu.",
                authored
            );

            Destroy(instance);
            return false;
        }

        view = authored;
        canvas = authored.canvas;
        canvasRect = authored.canvasRect != null
            ? authored.canvasRect
            : canvas.GetComponent<RectTransform>();

        leftPanel = authored.leftPanel;
        leftImage = authored.leftImage;
        rightOverlay = authored.rightOverlay;
        contentGroup = authored.contentGroup;
        contentCanvasGroup = authored.contentCanvasGroup;
        contentShift = authored.contentShift;
        buttonsRoot = authored.buttonsRoot;
        titleText = authored.titleText;

        menuAudio = authored.menuAudio != null
            ? authored.menuAudio
            : instance.AddComponent<MenuAudio>();

        menuAudio.Configure(config.audio);

        continueButton = authored.continueButton;
        settingsButton = authored.settingsButton;
        replayButton = authored.replayButton;
        mainMenuButton = authored.mainMenuButton;

        BindPauseButton(continueButton, BeginResume);
        BindPauseButton(settingsButton, OpenSettings);
        BindPauseButton(replayButton, ReplayChapter);
        BindPauseButton(mainMenuButton, ReturnToMainMenu);

        if (titleText != null)
            titleText.gameObject.SetActive(config.showTitle);

        if (authored.settingsPanel != null)
        {
            settingsPanel = authored.settingsPanel;

            SettingsPanelView settingsView =
                settingsPanel.GetComponentInChildren<SettingsPanelView>(true);

            settingsPanel.Build(
                settingsPanel.transform as RectTransform,
                config.settingsPanel,
                GameSettings.Config,
                menuAudio,
                HideSettings,
                settingsView
            );

            settingsPanel.Hide();
        }
        else
        {
            CreateSettingsPanel(leftPanel);
        }

        authoredLeftColor = leftImage.color;
        authoredRightColor = rightOverlay != null
            ? rightOverlay.color
            : config.rightDarkening;

        return true;
    }

    private void BindPauseButton(MenuButton button, Action action)
    {
        if (button == null)
            return;

        button.Bind(menuAudio);

        if (action != null)
            button.Clicked += action;
    }

    private MenuButton CreateButton(
        string label,
        int index,
        Action action)
    {
        float y = config.buttonOffset.y +
                  index * (config.buttonSize.y + config.buttonSpacing);

        RectTransform rect = UIHelper.CreateRect(
            label.Replace(" ", string.Empty),
            buttonsRoot,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(config.buttonOffset.x, -y),
            config.buttonSize
        );

        Image hitbox = UIHelper.AddPanel(
            rect,
            new Color(0f, 0f, 0f, 0f)
        );

        hitbox.raycastTarget = true;

        MenuButton button = rect.gameObject.AddComponent<MenuButton>();

        RectTransform labelRect = UIHelper.CreateStretchRect(
            "Label",
            rect,
            new Vector2(4f, 0f),
            new Vector2(-4f, 0f)
        );

        Text text = UIHelper.AddText(
            labelRect,
            config.buttonTextStyle,
            label
        );

        text.raycastTarget = false;

        button.Configure(
            text,
            null,
            hitbox,
            config.buttonStyle,
            menuAudio
        );

        if (action != null)
            button.Clicked += action;

        return button;
    }

    private void ReplayChapter()
    {
        if (!paused)
            return;

        bool started = SceneFlow.ReplayCurrent(
            config.replayShowsChapterIntro
        );

        if (!started)
        {
            Debug.LogWarning(
                "PauseManager: could not start replaying the current chapter."
            );
        }
    }

    private void ReturnToMainMenu()
    {
        if (!paused)
            return;

        bool started = SceneFlow.GoToMainMenu();

        if (!started)
        {
            Debug.LogWarning(
                "PauseManager: could not return to the main menu."
            );
        }
    }

    /// <summary>
    /// Called by SceneFlow before the loading screen takes over. The pause
    /// overlay disappears immediately, but gameplay is not resumed; the
    /// transition manager owns the time scale from this point.
    /// </summary>
    public void AbortForTransition()
    {
        if (!paused && state == State.Closed)
            return;

        bool wasPaused = paused;

        state = State.Closed;
        paused = false;
        settingsVisible = false;
        progress = 0f;

        if (settingsPanel != null)
            settingsPanel.Hide();

        if (canvas != null)
            canvas.gameObject.SetActive(false);

        UIState.PopModal(this);
        PlayerControl.Unlock(this);

        if (wasPaused)
        {
            Time.timeScale = previousTimeScale > 0f
                ? previousTimeScale
                : 1f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Force-closes the pause menu when the main menu scene becomes active.
    /// Progress and settings are untouched.
    /// </summary>
    public void HardResetForMenu()
    {
        state = State.Closed;
        paused = false;
        settingsVisible = false;
        progress = 0f;

        if (settingsPanel != null)
            settingsPanel.Hide();

        if (canvas != null)
            canvas.gameObject.SetActive(false);

        UIState.PopModal(this);
        PlayerControl.Unlock(this);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;

        if (paused)
        {
            UIState.PopModal(this);
            PlayerControl.Unlock(this);
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
    }
}
