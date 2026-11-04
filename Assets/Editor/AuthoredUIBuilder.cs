#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Converts the runtime-built screens into normal Canvas hierarchies. The
/// MainMenu Canvas lives in MainMenu.unity; the pause, dialogue, transition,
/// objective, toast and quest-log Canvases are prefabs in Resources so every
/// gameplay scene can instantiate them.
///
/// The builder never opens, saves or modifies Store.unity or City.unity.
/// </summary>
public static class AuthoredUIBuilder
{
    private const string ResourcesFolder = "Assets/Resources/Paralysis";
    private const string DialogueFolder = "Assets/Dialogue";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    private const string MainMenuConfigPath =
        ResourcesFolder + "/MainMenuConfig.asset";

    private const string ChapterDatabasePath =
        ResourcesFolder + "/ChapterDatabase.asset";

    private const string PauseMenuConfigPath =
        ResourcesFolder + "/PauseMenuConfig.asset";

    private const string SceneFlowConfigPath =
        ResourcesFolder + "/SceneFlowConfig.asset";

    private const string DialogueSystemConfigPath =
        ResourcesFolder + "/DialogueSystemConfig.asset";

    private const string DialogueDatabasePath =
        ResourcesFolder + "/DialogueDatabase.asset";

    private const string PausePrefabPath =
        ResourcesFolder + "/PauseMenu.prefab";

    private const string DialoguePrefabPath =
        ResourcesFolder + "/DialogueCanvas.prefab";

    private const string TransitionPrefabPath =
        ResourcesFolder + "/TransitionCanvas.prefab";

    private const string ObjectivePrefabPath =
        ResourcesFolder + "/ObjectiveCanvas.prefab";

    private const string ToastPrefabPath =
        ResourcesFolder + "/ToastCanvas.prefab";

    private const string QuestLogPrefabPath =
        ResourcesFolder + "/QuestLogCanvas.prefab";

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath) ?? ".";

    [MenuItem("Paralysis/Setup/Build Authored UI")]
    public static void BuildAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Authored UI?",
                "This rebuilds the editable MainMenu Canvas and the pause, " +
                "dialogue, transition, objective, toast and quest-log " +
                "prefabs. Store.unity and City.unity are not touched. " +
                "Existing edits to the generated Canvas are replaced.",
                "Build",
                "Cancel"))
        {
            return;
        }

        BuildAllInternal();
    }

    /// <summary>Entry point for the Temp/urp_request.txt dispatcher.</summary>
    public static void BuildAllBatch()
    {
        // A batch-mode editor starts with an untitled scene, and Unity refuses
        // to add a temporary scene additively while the active scene has never
        // been saved. Open a known saved scene first. In a normal editor
        // session the user's open scene is left alone.
        Scene active = EditorSceneManager.GetActiveScene();
        bool openedScene = false;

        if (Application.isBatchMode &&
            (!active.IsValid() || string.IsNullOrEmpty(active.path)))
        {
            string path = File.Exists(MainMenuScenePath)
                ? MainMenuScenePath
                : "Assets/Scenes/Home.unity";

            if (File.Exists(path))
            {
                active = EditorSceneManager.OpenScene(
                    path,
                    OpenSceneMode.Single
                );

                openedScene = true;
            }
        }

        try
        {
            BuildAllInternal();
        }
        finally
        {
            // Closing the only loaded scene is not supported by Unity. Leave
            // it open; the batch editor exits immediately below.
            if (openedScene && active.IsValid() && active.isLoaded &&
                SceneManager.sceneCount > 1)
            {
                EditorSceneManager.CloseScene(active, true);
            }
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    private static void BuildAllInternal()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "AuthoredUIBuilder: exit play mode before rebuilding the UI."
            );

            return;
        }

        StringBuilder report = new StringBuilder();

        try
        {
            EnsureFolder("Assets/Resources", "Paralysis");
            EnsureFolder("Assets", "Dialogue");

            MainMenuConfig menu = LoadOrCreate<MainMenuConfig>(
                MainMenuConfigPath
            );

            ChapterDatabase chapters = LoadOrCreate<ChapterDatabase>(
                ChapterDatabasePath
            );

            PauseMenuConfig pause = LoadOrCreate<PauseMenuConfig>(
                PauseMenuConfigPath
            );

            SceneFlowConfig flow = LoadOrCreate<SceneFlowConfig>(
                SceneFlowConfigPath
            );

            DialogueSystemConfig dialogueConfig =
                LoadOrCreate<DialogueSystemConfig>(
                    DialogueSystemConfigPath
                );

            DialogueDatabase dialogueDatabase =
                LoadOrCreate<DialogueDatabase>(
                    DialogueDatabasePath
                );

            AssignTitleFont(menu);

            GameObject pausePrefab = BuildPausePrefab(pause);
            GameObject dialoguePrefab = BuildDialoguePrefab();
            GameObject transitionPrefab = BuildTransitionPrefab(flow);
            GameObject objectivePrefab = BuildObjectivePrefab();
            GameObject toastPrefab = BuildToastPrefab();
            GameObject questLogPrefab = BuildQuestLogPrefab();

            pause.canvasPrefab = pausePrefab;
            flow.transitionPrefab = transitionPrefab;
            dialogueConfig.canvasPrefab = dialoguePrefab;
            dialogueConfig.useAuthoredCanvas = true;

            EditorUtility.SetDirty(pause);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(dialogueConfig);
            EditorUtility.SetDirty(dialogueDatabase);
            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(chapters);

            AssetDatabase.SaveAssets();

            BuildMainMenuScene(menu, chapters, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine("Pause prefab: " + PausePrefabPath);
            report.AppendLine("Dialogue prefab: " + DialoguePrefabPath);
            report.AppendLine("Transition prefab: " + TransitionPrefabPath);
            report.AppendLine("Objective prefab: " + ObjectivePrefabPath);
            report.AppendLine("Toast prefab: " + ToastPrefabPath);
            report.AppendLine("Quest log prefab: " + QuestLogPrefabPath);
            report.AppendLine("MainMenu Canvas: " + MainMenuScenePath);
            report.AppendLine("Done.");

            Debug.Log("AuthoredUIBuilder: " + report);
        }
        catch (Exception error)
        {
            report.AppendLine("ERROR: " + error);
            Debug.LogError("AuthoredUIBuilder: " + error);
        }
        finally
        {
            WriteReport(report.ToString());
        }
    }

    /// <summary>
    /// Creates the Canvas inside an existing MainMenu scene. Called by the
    /// scene builder so a freshly generated menu already has authored UI.
    /// </summary>
    public static void BuildMainMenuCanvas(
        Scene scene,
        MainMenuController controller,
        MainMenuConfig menu,
        ChapterDatabase chapters)
    {
        if (!scene.IsValid() || !scene.isLoaded || controller == null)
            return;

        GameObject existing = FindRoot(scene, "MainMenuCanvas");

        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        EnsureEventSystem(scene);

        GameObject root = new GameObject(
            "MainMenuCanvas",
            typeof(RectTransform)
        );

        SceneManager.MoveGameObjectToScene(root, scene);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        MainMenuView view = root.AddComponent<MainMenuView>();
        MenuAudio audio = root.AddComponent<MenuAudio>();
        audio.Configure(menu.audio);

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform background = UIHelper.CreateStretchRect(
            "Background",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        Image backgroundImage = UIHelper.AddPanel(
            background,
            menu.backgroundColor
        );

        backgroundImage.sprite = menu.backgroundSprite;
        backgroundImage.raycastTarget = true;

        RectTransform menuContent = UIHelper.CreateStretchRect(
            "MenuContent",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        Text titleText = null;

        if (menu.showTitle)
        {
            RectTransform titleRect = UIHelper.CreateRect(
                "Title",
                menuContent,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -menu.titleOffset.y),
                menu.titleSize
            );

            titleText = UIHelper.AddText(
                titleRect,
                menu.titleStyle,
                menu.titleText
            );

            titleText.raycastTarget = false;
            titleText.gameObject.AddComponent<GameFontIgnore>();
        }

        MenuButton newGame = CreateMainButton(
            "NewGame",
            menuContent,
            menu,
            audio,
            0,
            menu.newGameLabel,
            false
        );

        MenuButton continueButton = CreateMainButton(
            "Continue",
            menuContent,
            menu,
            audio,
            1,
            menu.continueLabel,
            true
        );

        MenuButton chaptersButton = CreateMainButton(
            "Chapters",
            menuContent,
            menu,
            audio,
            2,
            menu.chaptersLabel,
            false
        );

        MenuButton settingsButton = CreateMainButton(
            "Settings",
            menuContent,
            menu,
            audio,
            3,
            menu.settingsLabel,
            false
        );

        MenuButton quitButton = CreateMainButton(
            "Quit",
            menuContent,
            menu,
            audio,
            4,
            menu.quitLabel,
            false
        );

        RectTransform statusRect = UIHelper.CreateRect(
            "Status",
            menuContent,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            menu.statusOffset,
            menu.statusSize
        );

        Text statusText = UIHelper.AddText(
            statusRect,
            menu.statusStyle,
            string.Empty
        );

        statusText.raycastTarget = false;
        statusText.gameObject.SetActive(false);

        SettingsPanel settingsPanel = BuildSettingsPanel(
            rootRect,
            "SettingsHost",
            menu.settingsPanel,
            audio,
            new Vector2(
                menu.settingsPanel.panelWidth > 0f
                    ? menu.settingsPanel.panelWidth
                    : 900f,
                menu.settingsPanel.panelHeight > 0f
                    ? menu.settingsPanel.panelHeight
                    : 640f
            ),
            false
        );

        ChapterSelectionController chapterSelection =
            BuildChapterScreen(
                rootRect,
                menu,
                chapters,
                audio
            );

        view.canvas = canvas;
        view.menuContent = menuContent;
        view.backgroundImage = backgroundImage;
        view.titleText = titleText;
        view.statusText = statusText;
        view.newGameButton = newGame;
        view.continueButton = continueButton;
        view.chaptersButton = chaptersButton;
        view.settingsButton = settingsButton;
        view.quitButton = quitButton;
        view.settingsPanel = settingsPanel;
        view.chapterSelection = chapterSelection;
        view.menuAudio = audio;
        view.applyConfigText = false;

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty authoredView =
            serialized.FindProperty("authoredView");

        if (authoredView != null)
        {
            authoredView.objectReferenceValue = view;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void BuildMainMenuScene(
        MainMenuConfig menu,
        ChapterDatabase chapters,
        StringBuilder report)
    {
        if (!File.Exists(MainMenuScenePath))
        {
            report.AppendLine(
                "MainMenu scene missing; run Setup > Build Main Menu first."
            );

            return;
        }

        Scene previousActive = EditorSceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);

        bool openedHere = false;

        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(
                MainMenuScenePath,
                OpenSceneMode.Additive
            );

            openedHere = true;
        }

        EditorSceneManager.SetActiveScene(scene);

        MainMenuController controller =
            FindComponent<MainMenuController>(scene);

        if (controller == null)
        {
            report.AppendLine("MainMenu controller was not found.");
        }
        else
        {
            BuildMainMenuCanvas(scene, controller, menu, chapters);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine("Built MainMenu Canvas.");
        }

        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);
    }

    private static MenuButton CreateMainButton(
        string name,
        Transform parent,
        MainMenuConfig menu,
        MenuAudio audio,
        int index,
        string label,
        bool withSecondary)
    {
        float y = menu.buttonStart.y -
                  index * (menu.buttonSize.y + menu.buttonSpacing);

        RectTransform rect = UIHelper.CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(menu.buttonStart.x, y),
            menu.buttonSize
        );

        return AddButton(
            rect,
            label,
            menu.buttonTextStyle,
            menu.buttonStyle,
            audio,
            withSecondary,
            string.Empty,
            menu.secondaryTextStyle,
            menu.secondaryOffset,
            menu.secondarySize
        );
    }

    private static SettingsPanel BuildSettingsPanel(
        Transform parent,
        string hostName,
        SettingsPanelConfig config,
        MenuAudio audio,
        Vector2 hostSize,
        bool stretchHost)
    {
        GameObject host = new GameObject(hostName, typeof(RectTransform));
        host.transform.SetParent(parent, false);

        RectTransform hostRect = host.GetComponent<RectTransform>();

        if (stretchHost)
        {
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;
        }
        else
        {
            hostRect.anchorMin = new Vector2(0.5f, 0.5f);
            hostRect.anchorMax = new Vector2(0.5f, 0.5f);
            hostRect.pivot = new Vector2(0.5f, 0.5f);
            hostRect.anchoredPosition = config.panelAnchoredPosition;
            hostRect.sizeDelta = hostSize;
        }

        SettingsPanel panel = host.AddComponent<SettingsPanel>();
        SettingsPanelView view = host.AddComponent<SettingsPanelView>();

        RectTransform root = UIHelper.CreateStretchRect(
            "SettingsPanel",
            hostRect,
            Vector2.zero,
            Vector2.zero
        );

        VerticalLayoutGroup layout =
            root.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(
            Mathf.RoundToInt(config.padding.x),
            Mathf.RoundToInt(config.padding.x),
            Mathf.RoundToInt(config.padding.y),
            Mathf.RoundToInt(config.padding.y)
        );

        layout.spacing = config.rowSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Text titleText = CreateSettingsText(
            "Title",
            root,
            config.titleStyle,
            config.titleText,
            config.titleHeight
        );

        titleText.gameObject.SetActive(config.showTitle);

        Text sensitivityLabel = CreateSettingsText(
            "SensitivityLabel",
            root,
            config.labelStyle,
            config.sensitivityLabel,
            config.labelHeight
        );

        Slider slider = CreateSensitivityRow(
            root,
            config,
            out Text sensitivityValue
        );

        Text frameRateLabel = CreateSettingsText(
            "FrameRateLabel",
            root,
            config.labelStyle,
            config.frameRateLabel,
            config.labelHeight
        );

        MenuButton[] optionButtons = CreateOptionRow(
            root,
            config,
            audio
        );

        RectTransform spacer = UIHelper.CreateRect(
            "Spacer",
            root,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, config.backTopSpacing)
        );

        LayoutElement spacerLayout =
            spacer.gameObject.AddComponent<LayoutElement>();

        spacerLayout.minHeight = config.backTopSpacing;
        spacerLayout.flexibleHeight = 1f;
        spacerLayout.flexibleWidth = 1f;

        MenuButton backButton = CreateLayoutButton(
            "Back",
            root,
            config.backLabel,
            config.buttonTextStyle,
            config.backButtonStyle,
            audio,
            config.backWidth,
            config.backHeight,
            true
        );

        view.root = root;
        view.titleText = titleText;
        view.sensitivityLabel = sensitivityLabel;
        view.frameRateLabel = frameRateLabel;
        view.sensitivitySlider = slider;
        view.sensitivityValue = sensitivityValue;
        view.optionButtons = optionButtons;
        view.backButton = backButton;
        view.applyConfigText = false;

        root.gameObject.SetActive(false);
        return panel;
    }

    private static Text CreateSettingsText(
        string name,
        Transform parent,
        TextStyle style,
        string content,
        float height)
    {
        RectTransform rect = UIHelper.CreateRect(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, height)
        );

        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;

        Text text = UIHelper.AddText(rect, style, content);
        text.raycastTarget = false;

        return text;
    }

    private static Slider CreateSensitivityRow(
        Transform parent,
        SettingsPanelConfig config,
        out Text valueText)
    {
        float height = Mathf.Max(
            config.sliderHeight + 8f,
            config.sliderHeight
        );

        RectTransform row = UIHelper.CreateRect(
            "SensitivityRow",
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, height)
        );

        LayoutElement rowLayout =
            row.gameObject.AddComponent<LayoutElement>();

        rowLayout.minHeight = height;
        rowLayout.preferredHeight = height;
        rowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup rowGroup =
            row.gameObject.AddComponent<HorizontalLayoutGroup>();

        rowGroup.spacing = 14f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;

        GameObject sliderObject = new GameObject(
            "SensitivitySlider",
            typeof(RectTransform)
        );

        sliderObject.transform.SetParent(row, false);

        RectTransform sliderRect =
            sliderObject.GetComponent<RectTransform>();

        LayoutElement sliderLayout =
            sliderObject.AddComponent<LayoutElement>();

        sliderLayout.minHeight = config.sliderHeight;
        sliderLayout.preferredHeight = config.sliderHeight;
        sliderLayout.flexibleWidth = 1f;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform background = UIHelper.CreateRect(
            "Background",
            sliderRect,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(0f, 8f)
        );

        background.anchorMin = new Vector2(0f, 0.5f);
        background.anchorMax = new Vector2(1f, 0.5f);
        background.sizeDelta = new Vector2(0f, 8f);

        Image backgroundImage = UIHelper.AddPanel(
            background,
            config.sliderBackground
        );

        backgroundImage.raycastTarget = true;

        RectTransform fillArea = UIHelper.CreateStretchRect(
            "Fill Area",
            sliderRect,
            new Vector2(10f, 0f),
            new Vector2(-10f, 0f)
        );

        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(10f, -4f);
        fillArea.offsetMax = new Vector2(-10f, 4f);

        RectTransform fill = UIHelper.CreateStretchRect(
            "Fill",
            fillArea,
            Vector2.zero,
            Vector2.zero
        );

        Image fillImage = UIHelper.AddPanel(fill, config.sliderFill);
        fillImage.raycastTarget = false;

        RectTransform handleArea = UIHelper.CreateStretchRect(
            "Handle Slide Area",
            sliderRect,
            new Vector2(10f, 0f),
            new Vector2(-10f, 0f)
        );

        RectTransform handle = UIHelper.CreateRect(
            "Handle",
            handleArea,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(18f, 0f)
        );

        Image handleImage = UIHelper.AddPanel(
            handle,
            config.sliderHandle
        );

        handleImage.raycastTarget = true;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;

        RectTransform valueRect = UIHelper.CreateRect(
            "SensitivityValue",
            row,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(config.valueWidth, height)
        );

        LayoutElement valueLayout =
            valueRect.gameObject.AddComponent<LayoutElement>();

        valueLayout.minWidth = config.valueWidth;
        valueLayout.preferredWidth = config.valueWidth;
        valueLayout.minHeight = height;
        valueLayout.preferredHeight = height;

        valueText = UIHelper.AddText(
            valueRect,
            config.valueStyle,
            "0.0"
        );

        valueText.raycastTarget = false;

        return slider;
    }

    private static MenuButton[] CreateOptionRow(
        Transform parent,
        SettingsPanelConfig config,
        MenuAudio audio)
    {
        RectTransform row = UIHelper.CreateRect(
            "FrameRateOptions",
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, config.optionHeight)
        );

        LayoutElement rowLayout =
            row.gameObject.AddComponent<LayoutElement>();

        rowLayout.minHeight = config.optionHeight;
        rowLayout.preferredHeight = config.optionHeight;
        rowLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup rowGroup =
            row.gameObject.AddComponent<HorizontalLayoutGroup>();

        rowGroup.spacing = config.optionSpacing;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = false;

        int[] options = GameSettings.FrameRateOptions;
        MenuButton[] buttons = new MenuButton[options.Length];

        for (int i = 0; i < options.Length; i++)
        {
            buttons[i] = CreateLayoutButton(
                "FPS " + options[i],
                row,
                string.Format(config.fpsFormat, options[i]),
                config.buttonTextStyle,
                config.optionButtonStyle,
                audio,
                config.optionWidth,
                config.optionHeight,
                false
            );
        }

        return buttons;
    }

    private static MenuButton CreateLayoutButton(
        string name,
        Transform parent,
        string label,
        TextStyle textStyle,
        MenuButtonStyle buttonStyle,
        MenuAudio audio,
        float width,
        float height,
        bool flexibleWidth)
    {
        RectTransform rect = UIHelper.CreateRect(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(width, height)
        );

        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleWidth = flexibleWidth ? 1f : 0f;

        return AddButton(
            rect,
            label,
            textStyle,
            buttonStyle,
            audio,
            false,
            string.Empty,
            null,
            Vector2.zero,
            Vector2.zero
        );
    }

    internal static MenuButton AddButton(
        RectTransform rect,
        string label,
        TextStyle textStyle,
        MenuButtonStyle buttonStyle,
        MenuAudio audio,
        bool withSecondary,
        string secondaryContent,
        TextStyle secondaryStyle,
        Vector2 secondaryOffset,
        Vector2 secondarySize)
    {
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
            textStyle,
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
                secondaryOffset,
                secondarySize
            );

            secondaryText = UIHelper.AddText(
                secondaryRect,
                secondaryStyle ?? new TextStyle(),
                secondaryContent
            );

            secondaryText.raycastTarget = false;
            secondaryText.gameObject.SetActive(
                !string.IsNullOrEmpty(secondaryContent)
            );
        }

        button.Configure(
            labelText,
            secondaryText,
            hitbox,
            buttonStyle,
            audio
        );

        return button;
    }

    private static ChapterSelectionController BuildChapterScreen(
        Transform parent,
        MainMenuConfig menu,
        ChapterDatabase database,
        MenuAudio audio)
    {
        GameObject host = new GameObject(
            "ChapterSelectionHost",
            typeof(RectTransform)
        );

        host.transform.SetParent(parent, false);

        RectTransform hostRect = host.GetComponent<RectTransform>();
        hostRect.anchorMin = Vector2.zero;
        hostRect.anchorMax = Vector2.one;
        hostRect.offsetMin = Vector2.zero;
        hostRect.offsetMax = Vector2.zero;

        ChapterSelectionController controller =
            host.AddComponent<ChapterSelectionController>();

        ChapterSelectionView view =
            host.AddComponent<ChapterSelectionView>();

        RectTransform root = UIHelper.CreateStretchRect(
            "ChapterSelection",
            hostRect,
            Vector2.zero,
            Vector2.zero
        );

        Image backdrop = UIHelper.AddPanel(root, menu.chapterBackdrop);
        backdrop.raycastTarget = true;

        RectTransform titleRect = UIHelper.CreateRect(
            "ChapterTitle",
            root,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -menu.chapterTitleOffset.y),
            menu.chapterTitleSize
        );

        Text title = UIHelper.AddText(
            titleRect,
            menu.chapterScreenTitleStyle,
            menu.chapterScreenTitle
        );

        title.raycastTarget = false;

        RectTransform viewport = UIHelper.CreateRect(
            "ChapterViewport",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            menu.chapterListOffset,
            menu.chapterListSize
        );

        Image viewportImage = UIHelper.AddPanel(
            viewport,
            new Color(1f, 1f, 1f, 0f)
        );

        viewportImage.raycastTarget = true;
        UIHelper.AddMask(viewport);

        GameObject contentObject = new GameObject(
            "ChapterContent",
            typeof(RectTransform)
        );

        RectTransform content =
            contentObject.GetComponent<RectTransform>();

        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout =
            contentObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = menu.chapterButtonSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            contentObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;

        RectTransform templateRect = UIHelper.CreateRect(
            "ChapterButtonTemplate",
            content,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            Vector2.zero,
            new Vector2(0f, menu.chapterButtonHeight)
        );

        LayoutElement templateLayout =
            templateRect.gameObject.AddComponent<LayoutElement>();

        templateLayout.minHeight = menu.chapterButtonHeight;
        templateLayout.preferredHeight = menu.chapterButtonHeight;
        templateLayout.flexibleWidth = 1f;

        AddButton(
            templateRect,
            "CHAPTER",
            menu.chapterButtonTextStyle,
            menu.chapterButtonStyle,
            audio,
            false,
            string.Empty,
            null,
            Vector2.zero,
            Vector2.zero
        );

        templateRect.gameObject.SetActive(false);

        RectTransform emptyRect = UIHelper.CreateRect(
            "ChapterEmpty",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            menu.chapterListOffset,
            menu.chapterListSize
        );

        Text emptyText = UIHelper.AddText(
            emptyRect,
            menu.chapterEmptyStyle,
            menu.chapterEmptyLabel
        );

        emptyText.raycastTarget = false;
        emptyText.gameObject.SetActive(false);

        RectTransform backRect = UIHelper.CreateRect(
            "ChapterBack",
            root,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            menu.chapterBackOffset,
            menu.chapterBackSize
        );

        MenuButton backButton = AddButton(
            backRect,
            menu.chapterBackLabel,
            menu.chapterBackTextStyle,
            menu.chapterBackStyle,
            audio,
            false,
            string.Empty,
            null,
            Vector2.zero,
            Vector2.zero
        );

        view.root = root;
        view.titleText = title;
        view.emptyText = emptyText;
        view.viewport = viewport;
        view.content = content;
        view.chapterButtonTemplate = templateRect.gameObject;
        view.backButton = backButton;
        view.applyConfigText = false;

        root.gameObject.SetActive(false);
        return controller;
    }

    private static GameObject BuildPausePrefab(PauseMenuConfig config)
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();

        Scene temp = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(temp);

        GameObject root = new GameObject(
            "PauseCanvas",
            typeof(RectTransform)
        );

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        PauseMenuView view = root.AddComponent<PauseMenuView>();
        MenuAudio audio = root.AddComponent<MenuAudio>();
        audio.Configure(config.audio);

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform leftPanel = UIHelper.CreateStretchRect(
            "PauseLeftPanel",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        leftPanel.anchorMin = new Vector2(0f, 0f);
        leftPanel.anchorMax = new Vector2(config.leftWidthFraction, 1f);
        leftPanel.offsetMin = Vector2.zero;
        leftPanel.offsetMax = Vector2.zero;

        Image leftImage = UIHelper.AddPanel(
            leftPanel,
            config.leftPanelColor
        );

        leftImage.raycastTarget = true;

        RectTransform rightPanel = UIHelper.CreateStretchRect(
            "PauseRightDarkening",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        rightPanel.anchorMin = new Vector2(config.leftWidthFraction, 0f);
        rightPanel.anchorMax = new Vector2(1f, 1f);
        rightPanel.offsetMin = Vector2.zero;
        rightPanel.offsetMax = Vector2.zero;

        Image rightOverlay = UIHelper.AddPanel(
            rightPanel,
            config.rightDarkening
        );

        rightOverlay.raycastTarget = true;

        RectTransform contentGroup = UIHelper.CreateStretchRect(
            "PauseContent",
            leftPanel,
            Vector2.zero,
            Vector2.zero
        );

        CanvasGroup contentCanvasGroup =
            contentGroup.gameObject.AddComponent<CanvasGroup>();

        RectTransform contentShift = UIHelper.CreateStretchRect(
            "PauseContentShift",
            contentGroup,
            Vector2.zero,
            Vector2.zero
        );

        Text titleText = null;

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

        RectTransform buttonsRoot = UIHelper.CreateStretchRect(
            "PauseButtons",
            contentShift,
            Vector2.zero,
            Vector2.zero
        );

        MenuButton continueButton = CreatePauseButton(
            "Continue",
            buttonsRoot,
            config,
            audio,
            0,
            config.continueLabel
        );

        MenuButton settingsButton = CreatePauseButton(
            "Settings",
            buttonsRoot,
            config,
            audio,
            1,
            config.settingsLabel
        );

        MenuButton replayButton = CreatePauseButton(
            "Replay",
            buttonsRoot,
            config,
            audio,
            2,
            config.replayLabel
        );

        MenuButton mainMenuButton = CreatePauseButton(
            "MainMenu",
            buttonsRoot,
            config,
            audio,
            3,
            config.mainMenuLabel
        );

        SettingsPanel settingsPanel = BuildSettingsPanel(
            leftPanel,
            "PauseSettings",
            config.settingsPanel,
            audio,
            Vector2.zero,
            true
        );

        view.canvas = canvas;
        view.canvasRect = rootRect;
        view.leftPanel = leftPanel;
        view.leftImage = leftImage;
        view.rightOverlay = rightOverlay;
        view.contentGroup = contentGroup;
        view.contentCanvasGroup = contentCanvasGroup;
        view.contentShift = contentShift;
        view.buttonsRoot = buttonsRoot;
        view.titleText = titleText;
        view.continueButton = continueButton;
        view.settingsButton = settingsButton;
        view.replayButton = replayButton;
        view.mainMenuButton = mainMenuButton;
        view.settingsPanel = settingsPanel;
        view.menuAudio = audio;
        view.useAuthoredColors = true;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            PausePrefabPath
        );

        UnityEngine.Object.DestroyImmediate(root);
        EditorSceneManager.CloseScene(temp, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return prefab;
    }

    private static MenuButton CreatePauseButton(
        string name,
        Transform parent,
        PauseMenuConfig config,
        MenuAudio audio,
        int index,
        string label)
    {
        float y = config.buttonOffset.y +
                  index * (config.buttonSize.y + config.buttonSpacing);

        RectTransform rect = UIHelper.CreateRect(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(config.buttonOffset.x, -y),
            config.buttonSize
        );

        return AddButton(
            rect,
            label,
            config.buttonTextStyle,
            config.buttonStyle,
            audio,
            false,
            string.Empty,
            null,
            Vector2.zero,
            Vector2.zero
        );
    }

    private static GameObject BuildDialoguePrefab()
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();

        Scene temp = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(temp);

        GameObject root = new GameObject(
            "DialogueCanvas",
            typeof(RectTransform)
        );

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        DialogueView view = root.AddComponent<DialogueView>();

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform panel = UIHelper.CreateRect(
            "DialoguePanel",
            rootRect,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            Vector2.zero,
            new Vector2(1920f, 260f)
        );

        Image panelImage = UIHelper.AddPanel(
            panel,
            new Color(0.02f, 0.02f, 0.02f, 0.92f)
        );

        panelImage.raycastTarget = true;
        UIHelper.AddMask(panel);

        CanvasGroup panelGroup =
            panel.gameObject.AddComponent<CanvasGroup>();

        panelGroup.alpha = 1f;
        panelGroup.blocksRaycasts = true;

        RectTransform portraitRect = UIHelper.CreateRect(
            "Portrait",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(26f, -26f),
            new Vector2(160f, 160f)
        );

        Image portrait = UIHelper.AddPanel(
            portraitRect,
            Color.white
        );

        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portraitRect.gameObject.SetActive(false);

        TextStyle speakerStyle = new TextStyle
        {
            fontSize = 24,
            bold = true,
            color = new Color(1f, 0.84f, 0.42f),
            alignment = TextAnchor.UpperLeft
        };

        RectTransform speakerRect = UIHelper.CreateRect(
            "Speaker",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(210f, -26f),
            new Vector2(1684f, 30f)
        );

        Text speakerText = UIHelper.AddText(
            speakerRect,
            speakerStyle,
            string.Empty
        );

        speakerText.raycastTarget = false;

        TextStyle bodyStyle = new TextStyle
        {
            fontSize = 28,
            bold = false,
            color = new Color(0.95f, 0.95f, 0.95f),
            alignment = TextAnchor.UpperLeft
        };

        RectTransform bodyRect = UIHelper.CreateRect(
            "Body",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(210f, -56f),
            new Vector2(1684f, 178f)
        );

        Text bodyText = UIHelper.AddText(
            bodyRect,
            bodyStyle,
            string.Empty
        );

        bodyText.raycastTarget = false;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform promptRect = UIHelper.CreateRect(
            "AdvancePrompt",
            panel,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-26f, 14f),
            new Vector2(80f, 30f)
        );

        Text promptText = UIHelper.AddText(
            promptRect,
            new TextStyle
            {
                fontSize = 22,
                bold = true,
                color = new Color(0.75f, 0.75f, 0.75f),
                alignment = TextAnchor.LowerRight
            },
            "▼"
        );

        promptText.raycastTarget = false;
        promptRect.gameObject.SetActive(false);

        AudioSource voiceSource = CreateAudioSource(
            "DialogueVoice",
            root.transform
        );

        AudioSource sfxSource = CreateAudioSource(
            "DialogueSfx",
            root.transform
        );

        view.canvas = canvas;
        view.panel = panel;
        view.panelGroup = panelGroup;
        view.speakerRect = speakerRect;
        view.speakerText = speakerText;
        view.bodyRect = bodyRect;
        view.bodyText = bodyText;
        view.portraitImage = portrait;
        view.advancePrompt = promptRect.gameObject;
        view.voiceSource = voiceSource;
        view.sfxSource = sfxSource;
        view.applyStyles = false;
        view.applySpeakerColors = true;
        view.applyRuntimeLayout = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            DialoguePrefabPath
        );

        UnityEngine.Object.DestroyImmediate(root);
        EditorSceneManager.CloseScene(temp, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return prefab;
    }

    private static GameObject BuildTransitionPrefab(SceneFlowConfig config)
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();

        Scene temp = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(temp);

        GameObject root = new GameObject(
            "SceneTransitionCanvas",
            typeof(RectTransform)
        );

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        TransitionView view = root.AddComponent<TransitionView>();

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform blackRoot = UIHelper.CreateStretchRect(
            "TransitionBlack",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        Image blackImage = UIHelper.AddPanel(blackRoot, Color.black);
        blackImage.raycastTarget = true;

        CanvasGroup blackGroup =
            blackRoot.gameObject.AddComponent<CanvasGroup>();

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

        Text loadingText = UIHelper.AddText(
            loadingRect,
            config.loadingStyle,
            config.loadingText
        );

        loadingText.raycastTarget = false;
        loadingText.gameObject.AddComponent<GameFontIgnore>();

        LoadingIndicator indicator =
            loadingText.gameObject.AddComponent<LoadingIndicator>();

        indicator.Configure(
            loadingText,
            config.loadingText,
            config.animateDots,
            config.dotStepInterval
        );

        // Configure runs the dot animation immediately, which would make the
        // saved label "LOADING.  ". Keep the authored base text clean so the
        // runtime does not append a second set of dots.
        loadingText.text = config.loadingText;

        RectTransform introRoot = UIHelper.CreateStretchRect(
            "ChapterIntro",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        Image introBackground = UIHelper.AddPanel(
            introRoot,
            Color.black
        );

        introBackground.raycastTarget = true;

        CanvasGroup introGroup =
            introRoot.gameObject.AddComponent<CanvasGroup>();

        introGroup.alpha = 0f;
        introGroup.blocksRaycasts = true;

        ChapterIntroScreen introScreen =
            introRoot.gameObject.AddComponent<ChapterIntroScreen>();

        ChapterIntroView introView =
            introRoot.gameObject.AddComponent<ChapterIntroView>();

        ChapterIntroStyle style = config.defaultIntroStyle ??
                                  new ChapterIntroStyle();

        Text numberText = CreateIntroText(
            "ChapterNumber",
            introRoot,
            style.numberStyle,
            style.numberAnchor,
            style.numberAnchoredPosition,
            style.numberSize,
            "CHAPTER 1"
        );

        Text titleText = CreateIntroText(
            "ChapterTitle",
            introRoot,
            style.titleStyle,
            style.titleAnchor,
            style.titleAnchoredPosition,
            style.titleSize,
            "CHAPTER TITLE"
        );

        Text subtitleText = CreateIntroText(
            "ChapterSubtitle",
            introRoot,
            style.subtitleStyle,
            style.subtitleAnchor,
            style.subtitleAnchoredPosition,
            style.subtitleSize,
            "SUBTITLE"
        );

        introView.root = introRoot;
        introView.group = introGroup;
        introView.numberText = numberText;
        introView.titleText = titleText;
        introView.subtitleText = subtitleText;
        introView.applyChapterStyles = false;

        introScreen.SetView(introView);

        AudioSource audioSource = CreateAudioSource(
            "TransitionAudio",
            root.transform
        );

        audioSource.ignoreListenerPause = true;

        view.canvas = canvas;
        view.canvasRect = rootRect;
        view.blackRoot = blackRoot;
        view.blackGroup = blackGroup;
        view.loadingText = loadingText;
        view.loadingIndicator = indicator;
        view.useAuthoredLoadingText = true;
        view.introScreen = introScreen;
        view.audioSource = audioSource;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            TransitionPrefabPath
        );

        UnityEngine.Object.DestroyImmediate(root);
        EditorSceneManager.CloseScene(temp, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return prefab;
    }

    /// <summary>
    /// Editable top-left objective HUD. The manager still owns the objective
    /// list; this prefab only supplies the look.
    /// </summary>
    private static GameObject BuildObjectivePrefab()
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();

        Scene temp = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(temp);

        GameObject root = new GameObject(
            "ObjectiveCanvas",
            typeof(RectTransform)
        );

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        ObjectiveView view = root.AddComponent<ObjectiveView>();

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform panel = UIHelper.CreateRect(
            "ObjectivePanel",
            rootRect,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(40f, -40f),
            new Vector2(560f, 130f)
        );

        UIHelper.AddPanel(panel, new Color(0f, 0f, 0f, 0.55f));
        UIHelper.AddMask(panel);

        RectTransform headerRect = UIHelper.CreateRect(
            "Header",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -14f),
            new Vector2(520f, 26f)
        );

        Text headerText = UIHelper.AddText(
            headerRect,
            new TextStyle
            {
                fontSize = 20,
                bold = true,
                color = new Color(1f, 0.84f, 0.42f),
                alignment = TextAnchor.UpperLeft
            },
            "OBJECTIVE"
        );

        headerText.alignment = TextAnchor.UpperLeft;
        headerText.raycastTarget = false;

        RectTransform bodyRect = UIHelper.CreateRect(
            "Body",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -44f),
            new Vector2(520f, 72f)
        );

        Text bodyText = UIHelper.AddText(
            bodyRect,
            new TextStyle
            {
                fontSize = 26,
                bold = false,
                color = Color.white,
                alignment = TextAnchor.UpperLeft
            },
            string.Empty
        );

        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        bodyText.raycastTarget = false;

        view.canvas = canvas;
        view.panel = panel;
        view.headerText = headerText;
        view.bodyText = bodyText;
        view.completedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        view.applyStyles = false;

        panel.gameObject.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            ObjectivePrefabPath
        );

        UnityEngine.Object.DestroyImmediate(root);
        EditorSceneManager.CloseScene(temp, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return prefab;
    }

    /// <summary>
    /// Editable instruction toast. ToastUI still controls the message and
    /// timer; this prefab supplies the panel and text.
    /// </summary>
    private static GameObject BuildToastPrefab()
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();

        Scene temp = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(temp);

        GameObject root = new GameObject(
            "ToastCanvas",
            typeof(RectTransform)
        );

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        ToastView view = root.AddComponent<ToastView>();

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform panel = UIHelper.CreateRect(
            "ToastPanel",
            rootRect,
            new Vector2(0.5f, 0.44f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1180f, 90f)
        );

        UIHelper.AddPanel(panel, new Color(0f, 0f, 0f, 0.78f));
        UIHelper.AddMask(panel);

        CanvasGroup panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        panelGroup.alpha = 1f;
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;

        RectTransform textRect = UIHelper.CreateStretchRect(
            "ToastText",
            panel,
            new Vector2(18f, 6f),
            new Vector2(-18f, -6f)
        );

        Text text = UIHelper.AddText(
            textRect,
            new TextStyle
            {
                fontSize = 26,
                bold = false,
                color = Color.white,
                alignment = TextAnchor.MiddleCenter
            },
            string.Empty
        );

        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        view.canvas = canvas;
        view.panel = panel;
        view.text = text;
        view.panelGroup = panelGroup;
        view.applyStyles = false;

        panel.gameObject.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            ToastPrefabPath
        );

        UnityEngine.Object.DestroyImmediate(root);
        EditorSceneManager.CloseScene(temp, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return prefab;
    }

    /// <summary>
    /// Editable M-key mission log. The prefab keeps the same dim background,
    /// centred panel, scrollable body and sorting order as the runtime log.
    /// </summary>
    private static GameObject BuildQuestLogPrefab()
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();

        Scene temp = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(temp);

        GameObject root = new GameObject(
            "QuestLogCanvas",
            typeof(RectTransform)
        );

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        QuestLogView view = root.AddComponent<QuestLogView>();

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform dim = UIHelper.CreateStretchRect(
            "Dim",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        UIHelper.AddPanel(dim, new Color(0f, 0f, 0f, 0.75f));

        RectTransform panel = UIHelper.CreateRect(
            "QuestPanel",
            rootRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(880f, 700f)
        );

        UIHelper.AddPanel(panel, new Color(0.06f, 0.06f, 0.08f, 0.97f));

        RectTransform titleRect = UIHelper.CreateRect(
            "Title",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(40f, -40f),
            new Vector2(800f, 56f)
        );

        Text titleText = UIHelper.AddText(
            titleRect,
            new TextStyle
            {
                fontSize = 40,
                bold = true,
                color = new Color(1f, 0.84f, 0.42f),
                alignment = TextAnchor.UpperLeft
            },
            "MISSION LOG"
        );

        titleText.alignment = TextAnchor.UpperLeft;
        titleText.raycastTarget = false;

        RectTransform viewport = UIHelper.CreateRect(
            "Viewport",
            panel,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(40f, 40f),
            new Vector2(800f, 540f)
        );

        // ScrollRect needs a raycast target to receive the mouse wheel.
        Image viewportBackground = viewport.gameObject.AddComponent<Image>();
        viewportBackground.color = new Color(0f, 0f, 0f, 0.001f);
        viewportBackground.raycastTarget = true;

        viewport.gameObject.AddComponent<RectMask2D>();

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewport, false);

        RectTransform content = contentObject.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        Text bodyText = contentObject.AddComponent<Text>();
        bodyText.text = string.Empty;
        new TextStyle
        {
            fontSize = 26,
            bold = false,
            color = Color.white,
            alignment = TextAnchor.UpperLeft
        }.Apply(bodyText);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.raycastTarget = false;

        ContentSizeFitter fitter =
            contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        view.canvas = canvas;
        view.panel = panel;
        view.titleText = titleText;
        view.bodyText = bodyText;
        view.scrollRect = scrollRect;
        view.applyStyles = false;

        root.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            QuestLogPrefabPath
        );

        UnityEngine.Object.DestroyImmediate(root);
        EditorSceneManager.CloseScene(temp, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return prefab;
    }

    private static Text CreateIntroText(
        string name,
        Transform parent,
        TextStyle style,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        string content)
    {
        RectTransform rect = UIHelper.CreateRect(
            name,
            parent,
            anchor,
            anchor,
            position,
            size
        );

        Text text = UIHelper.AddText(rect, style, content);
        text.raycastTarget = false;
        text.gameObject.AddComponent<GameFontIgnore>();

        return text;
    }

    private static AudioSource CreateAudioSource(
        string name,
        Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;

        return source;
    }

    internal static void EnsureEventSystem(Scene scene)
    {
        EventSystem existing = FindComponent<EventSystem>(scene);

        if (existing != null)
            return;

        GameObject go = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );

        SceneManager.MoveGameObjectToScene(go, scene);
    }

    private static T FindComponent<T>(Scene scene)
        where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            T found = roots[i].GetComponentInChildren<T>(true);

            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (string.Equals(
                    roots[i].name,
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return roots[i];
            }
        }

        return null;
    }

    private static void AssignTitleFont(MainMenuConfig menu)
    {
        if (menu.titleStyle == null)
            menu.titleStyle = new TextStyle();

        if (menu.titleStyle.font != null)
            return;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/Resources/Fonts/PlaceholderTitle.ttf"
        );

        if (font == null)
            font = Resources.Load<Font>("Fonts/PlaceholderTitle");

        if (font == null)
            font = GameFont.Default;

        menu.titleStyle.font = font;
    }

    private static T LoadOrCreate<T>(string path)
        where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);

        return asset;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (AssetDatabase.IsValidFolder(parent + "/" + child))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
        {
            int slash = parent.LastIndexOf('/');

            if (slash > 0)
                EnsureFolder(parent.Substring(0, slash), parent.Substring(slash + 1));
            else
                AssetDatabase.CreateFolder("Assets", child);
        }

        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void WriteReport(string report)
    {
        try
        {
            string logs = Path.Combine(ProjectRoot, "Logs");

            if (!Directory.Exists(logs))
                Directory.CreateDirectory(logs);

            File.WriteAllText(
                Path.Combine(logs, "authored_ui_build.txt"),
                report
            );
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "AuthoredUIBuilder: could not write report: " +
                error.Message
            );
        }
    }
}
#endif
