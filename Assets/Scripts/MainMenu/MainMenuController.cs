using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds and runs the title screen entirely from MainMenuConfig. The scene is
/// intentionally tiny: a camera, this controller and an assigned config asset.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private enum Mode
    {
        Menu,
        Chapters,
        Settings
    }

    private const string ConfigResourcePath = "Paralysis/MainMenuConfig";

    [SerializeField] private MainMenuConfig config;
    [SerializeField] private ChapterDatabase chapterDatabase;
    [SerializeField] private MainMenuView authoredView;

    private Canvas canvas;
    private RectTransform menuContent;
    private Text titleText;
    private Text statusText;
    private MenuButton newGameButton;
    private MenuButton continueButton;
    private MenuButton chaptersButton;
    private MenuButton settingsButton;
    private MenuButton quitButton;
    private SettingsPanel settingsPanel;
    private ChapterSelectionController chapterSelection;
    private MenuAudio menuAudio;

    private Mode mode = Mode.Menu;
    private float statusHideAt;

    public Text TitleText => titleText;
    public MenuButton NewGameButton => newGameButton;
    public MenuButton ContinueButton => continueButton;
    public MenuButton ChaptersButton => chaptersButton;
    public MenuButton SettingsButton => settingsButton;
    public MenuButton QuitButton => quitButton;
    public SettingsPanel Settings => settingsPanel;
    public ChapterSelectionController ChapterSelection => chapterSelection;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResolveConfig();

        // The menu always reads the latest saved progress, but never touches
        // the separate settings file.
        GameProgress.Reload();
        GameSettings.EnsureInitialized();

        BuildUI();
        ShowMenu();
    }

    private void ResolveConfig()
    {
        if (config == null)
            config = Resources.Load<MainMenuConfig>(ConfigResourcePath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MainMenuConfig>();
            config.name = "RuntimeMainMenuConfig";
        }

        if (chapterDatabase == null)
            chapterDatabase = ChapterDatabase.Load();

        if (config.titleStyle.font == null)
        {
            config.titleStyle.font = Resources.Load<Font>(
                "Fonts/PlaceholderTitle"
            );
        }

        if (config.audio == null)
            config.audio = new MenuAudioConfig();

        if (config.settingsPanel == null)
            config.settingsPanel = new SettingsPanelConfig();
    }

    private void BuildUI()
    {
        if (authoredView == null)
            authoredView = FindAnyObjectByType<MainMenuView>();

        if (authoredView != null)
        {
            BindAuthoredView();
            return;
        }

        canvas = UIHelper.CreateCanvas("MainMenuCanvas", 100, false);

        RectTransform background = UIHelper.CreateStretchRect(
            "Background",
            canvas.transform,
            Vector2.zero,
            Vector2.zero
        );

        Image backgroundImage = UIHelper.AddPanel(
            background,
            config.backgroundColor
        );

        backgroundImage.raycastTarget = true;

        menuContent = UIHelper.CreateStretchRect(
            "MenuContent",
            canvas.transform,
            Vector2.zero,
            Vector2.zero
        );

        if (config.showTitle)
        {
            RectTransform titleRect = UIHelper.CreateRect(
                "Title",
                menuContent,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -config.titleOffset.y),
                config.titleSize
            );

            titleText = UIHelper.AddText(
                titleRect,
                config.titleStyle,
                config.titleText
            );

            titleText.raycastTarget = false;
            titleText.gameObject.AddComponent<GameFontIgnore>();
        }

        menuAudio = gameObject.AddComponent<MenuAudio>();
        menuAudio.Configure(config.audio);

        newGameButton = CreateMenuButton(
            config.newGameLabel,
            0,
            false,
            StartNewGame
        );

        continueButton = CreateMenuButton(
            config.continueLabel,
            1,
            true,
            ContinueGame
        );

        chaptersButton = CreateMenuButton(
            config.chaptersLabel,
            2,
            false,
            OpenChapters
        );

        settingsButton = CreateMenuButton(
            config.settingsLabel,
            3,
            false,
            OpenSettings
        );

        quitButton = CreateMenuButton(
            config.quitLabel,
            4,
            false,
            QuitGame
        );

        RectTransform statusRect = UIHelper.CreateRect(
            "Status",
            menuContent,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            config.statusOffset,
            config.statusSize
        );

        statusText = UIHelper.AddText(
            statusRect,
            config.statusStyle,
            string.Empty
        );

        statusText.raycastTarget = false;
        statusText.gameObject.SetActive(false);

        BuildSettingsPanel();
        BuildChapterSelection();
    }

    /// <summary>
    /// Uses the Canvas that lives in MainMenu.unity. The controller only
    /// supplies behaviour; every visible object is authored and editable.
    /// </summary>
    private void BindAuthoredView()
    {
        UIHelper.EnsureEventSystem();

        canvas = authoredView.canvas != null
            ? authoredView.canvas
            : authoredView.GetComponentInParent<Canvas>();

        menuContent = authoredView.menuContent;
        titleText = authoredView.titleText;
        statusText = authoredView.statusText;

        if (authoredView.backgroundImage != null &&
            config.backgroundSprite != null)
        {
            authoredView.backgroundImage.sprite = config.backgroundSprite;
        }

        menuAudio = authoredView.menuAudio != null
            ? authoredView.menuAudio
            : gameObject.AddComponent<MenuAudio>();

        menuAudio.Configure(config.audio);

        if (titleText != null)
        {
            titleText.gameObject.SetActive(config.showTitle);

            if (authoredView.applyConfigText)
            {
                titleText.text = config.titleText;
                config.titleStyle.Apply(titleText);
            }

            if (titleText.GetComponent<GameFontIgnore>() == null)
                titleText.gameObject.AddComponent<GameFontIgnore>();
        }

        if (statusText == null && menuContent != null)
        {
            RectTransform statusRect = UIHelper.CreateRect(
                "Status",
                menuContent,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                config.statusOffset,
                config.statusSize
            );

            statusText = UIHelper.AddText(
                statusRect,
                config.statusStyle,
                string.Empty
            );
        }

        if (statusText != null)
        {
            statusText.raycastTarget = false;
            statusText.gameObject.SetActive(false);
        }

        newGameButton = authoredView.newGameButton;
        continueButton = authoredView.continueButton;
        chaptersButton = authoredView.chaptersButton;
        settingsButton = authoredView.settingsButton;
        quitButton = authoredView.quitButton;

        BindButton(newGameButton, StartNewGame);
        BindButton(continueButton, ContinueGame);
        BindButton(chaptersButton, OpenChapters);
        BindButton(settingsButton, OpenSettings);
        BindButton(quitButton, QuitGame);

        if (authoredView.settingsPanel != null)
        {
            settingsPanel = authoredView.settingsPanel;

            SettingsPanelView settingsView =
                settingsPanel.GetComponentInChildren<SettingsPanelView>(true);

            settingsPanel.Build(
                settingsPanel.transform as RectTransform,
                config.settingsPanel,
                GameSettings.Config,
                menuAudio,
                ShowMenu,
                settingsView
            );

            settingsPanel.Hide();
        }
        else
        {
            BuildSettingsPanel();
        }

        if (authoredView.chapterSelection != null)
        {
            chapterSelection = authoredView.chapterSelection;

            ChapterSelectionView chapterView =
                chapterSelection.GetComponentInChildren<ChapterSelectionView>(
                    true
                );

            chapterSelection.Build(
                chapterSelection.transform as RectTransform,
                config,
                chapterDatabase,
                menuAudio,
                ShowMenu,
                ShowStatus,
                chapterView
            );

            chapterSelection.Hide();
        }
        else
        {
            BuildChapterSelection();
        }
    }

    private void BindButton(MenuButton button, Action action)
    {
        if (button == null)
            return;

        button.Bind(menuAudio);

        if (action != null)
            button.Clicked += action;
    }

    private MenuButton CreateMenuButton(
        string label,
        int index,
        bool withSecondary,
        Action action)
    {
        float y = config.buttonStart.y -
                  index * (config.buttonSize.y + config.buttonSpacing);

        RectTransform rect = UIHelper.CreateRect(
            label.Replace(" ", string.Empty),
            menuContent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(config.buttonStart.x, y),
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
            Vector2.zero,
            Vector2.zero
        );

        Text labelText = UIHelper.AddText(
            labelRect,
            config.buttonTextStyle,
            label
        );

        labelText.raycastTarget = false;

        Text secondaryText = null;

        if (withSecondary)
        {
            RectTransform secondaryRect = UIHelper.CreateRect(
                "Secondary",
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                config.secondaryOffset,
                config.secondarySize
            );

            secondaryText = UIHelper.AddText(
                secondaryRect,
                config.secondaryTextStyle,
                string.Empty
            );

            secondaryText.raycastTarget = false;
        }

        button.Configure(
            labelText,
            secondaryText,
            hitbox,
            config.buttonStyle,
            menuAudio
        );

        if (action != null)
            button.Clicked += action;

        return button;
    }

    private void BuildSettingsPanel()
    {
        float width = config.settingsPanel.panelWidth > 0f
            ? config.settingsPanel.panelWidth
            : 900f;

        float height = config.settingsPanel.panelHeight > 0f
            ? config.settingsPanel.panelHeight
            : 640f;

        RectTransform host = UIHelper.CreateRect(
            "SettingsHost",
            canvas.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(width, height)
        );

        settingsPanel = host.gameObject.AddComponent<SettingsPanel>();

        settingsPanel.Build(
            host,
            config.settingsPanel,
            GameSettings.Config,
            menuAudio,
            ShowMenu
        );

        settingsPanel.Hide();
    }

    private void BuildChapterSelection()
    {
        GameObject host = new GameObject(
            "ChapterSelectionHost",
            typeof(RectTransform)
        );

        host.transform.SetParent(canvas.transform, false);

        RectTransform rect = host.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        chapterSelection =
            host.AddComponent<ChapterSelectionController>();

        chapterSelection.Build(
            rect,
            config,
            chapterDatabase,
            menuAudio,
            ShowMenu,
            ShowStatus
        );

        chapterSelection.Hide();
    }

    private void Update()
    {
        if (statusText != null &&
            statusText.gameObject.activeSelf &&
            Time.unscaledTime >= statusHideAt)
        {
            statusText.gameObject.SetActive(false);
        }

        if (mode != Mode.Menu &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMenu();
        }
    }

    public void ShowMenu()
    {
        mode = Mode.Menu;

        if (menuContent != null)
            menuContent.gameObject.SetActive(true);

        if (chapterSelection != null)
            chapterSelection.Hide();

        if (settingsPanel != null)
            settingsPanel.Hide();

        RefreshContinueAvailability();
    }

    private void ShowChapters()
    {
        mode = Mode.Chapters;

        if (menuContent != null)
            menuContent.gameObject.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.Hide();

        if (chapterSelection != null)
            chapterSelection.Show();
    }

    private void ShowSettings()
    {
        mode = Mode.Settings;

        if (menuContent != null)
            menuContent.gameObject.SetActive(false);

        if (chapterSelection != null)
            chapterSelection.Hide();

        if (settingsPanel != null)
            settingsPanel.Show();
    }

    private void RefreshContinueAvailability()
    {
        if (continueButton == null)
            return;

        bool available = GameProgress.HasProgress;

        continueButton.SetInteractable(available);
        continueButton.SetSecondaryLabel(
            available ? string.Empty : config.noSaveLabel
        );
    }

    private void StartNewGame()
    {
        ChapterDatabase database = ResolveDatabase();

        if (database == null)
        {
            ShowStatus("NO CHAPTER DATABASE FOUND");
            return;
        }

        ChapterDefinition first = database.FirstChapter;

        if (first == null)
        {
            ShowStatus("NO CHAPTERS CONFIGURED");
            return;
        }

        if (string.IsNullOrEmpty(first.sceneName) ||
            !Application.CanStreamedLevelBeLoaded(first.sceneName))
        {
            ShowStatus(
                "SCENE '" + first.sceneName + "' IS NOT IN BUILD SETTINGS"
            );

            return;
        }

        // BeginNewGame resets story progress only. Settings are a separate
        // file and are never touched here.
        if (!SceneFlow.NewGame(
                first,
                config.showChapterIntroOnNewGame))
        {
            ShowStatus(
                string.IsNullOrEmpty(SceneFlow.LastError)
                    ? "COULD NOT START A NEW GAME"
                    : SceneFlow.LastError
            );
        }
    }

    private void ContinueGame()
    {
        if (!GameProgress.HasProgress)
        {
            RefreshContinueAvailability();
            ShowStatus(config.noSaveLabel);
            return;
        }

        if (!SceneFlow.Continue(config.showChapterIntroOnContinue))
        {
            ShowStatus(
                string.IsNullOrEmpty(SceneFlow.LastError)
                    ? "COULD NOT LOAD THE SAVED CHAPTER"
                    : SceneFlow.LastError
            );
        }
    }

    private void OpenChapters()
    {
        ChapterDatabase database = ResolveDatabase();

        if (database == null || database.FirstChapter == null)
        {
            ShowStatus("NO CHAPTERS CONFIGURED");
            return;
        }

        ShowChapters();
    }

    private void OpenSettings()
    {
        ShowSettings();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private ChapterDatabase ResolveDatabase()
    {
        if (chapterDatabase == null)
            chapterDatabase = ChapterDatabase.Load();

        return chapterDatabase;
    }

    private void ShowStatus(string message)
    {
        if (statusText == null || string.IsNullOrEmpty(message))
            return;

        statusText.text = message;
        statusText.gameObject.SetActive(true);
        statusHideAt = Time.unscaledTime + config.statusDuration;
    }
}
