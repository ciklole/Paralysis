using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Data-driven chapter list. It reads ChapterDatabase, respects the database's
/// development/player availability mode, and scrolls when there are more
/// chapters than fit on screen.
/// </summary>
public class ChapterSelectionController : MonoBehaviour
{
    private MainMenuConfig config;
    private ChapterDatabase database;
    private MenuAudio audioPlayer;
    private Action backRequested;
    private Action<string> statusReported;

    private RectTransform root;
    private RectTransform content;
    private Text emptyText;
    private MenuButton backButton;
    private ChapterSelectionView view;
    private GameObject chapterButtonTemplate;
    private bool built;
    private bool listBuilt;

    private readonly List<ChapterDefinition> chapters =
        new List<ChapterDefinition>();

    private readonly List<MenuButton> chapterButtons =
        new List<MenuButton>();

    public bool IsOpen =>
        root != null && root.gameObject.activeSelf;

    public int ChapterCount => chapters.Count;
    public IReadOnlyList<MenuButton> ChapterButtons => chapterButtons;

    public void Build(
        RectTransform parent,
        MainMenuConfig menuConfig,
        ChapterDatabase chapterDatabase,
        MenuAudio audio,
        Action onBack,
        Action<string> onStatus,
        ChapterSelectionView authoredView = null)
    {
        if (built)
            return;

        config = menuConfig ?? new MainMenuConfig();
        database = chapterDatabase;
        audioPlayer = audio;
        backRequested = onBack;
        statusReported = onStatus;
        view = authoredView;

        if (view != null)
        {
            BindAuthored(parent);
            return;
        }

        root = UIHelper.CreateStretchRect(
            "ChapterSelection",
            parent,
            Vector2.zero,
            Vector2.zero
        );

        Image backdrop = UIHelper.AddPanel(
            root,
            config.chapterBackdrop
        );

        backdrop.raycastTarget = true;

        RectTransform titleRect = UIHelper.CreateRect(
            "ChapterTitle",
            root,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(
                0f,
                -config.chapterTitleOffset.y
            ),
            config.chapterTitleSize
        );

        Text title = UIHelper.AddText(
            titleRect,
            config.chapterScreenTitleStyle,
            config.chapterScreenTitle
        );

        title.raycastTarget = false;

        RectTransform viewport = UIHelper.CreateRect(
            "ChapterViewport",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            config.chapterListOffset,
            config.chapterListSize
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

        content = contentObject.GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout =
            contentObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = config.chapterButtonSpacing;
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

        RectTransform emptyRect = UIHelper.CreateRect(
            "ChapterEmpty",
            root,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            config.chapterListOffset,
            config.chapterListSize
        );

        emptyText = UIHelper.AddText(
            emptyRect,
            config.chapterEmptyStyle,
            config.chapterEmptyLabel
        );

        emptyText.raycastTarget = false;
        emptyText.gameObject.SetActive(false);

        RectTransform backRect = UIHelper.CreateRect(
            "ChapterBack",
            root,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            config.chapterBackOffset,
            config.chapterBackSize
        );

        Image backHitbox = UIHelper.AddPanel(
            backRect,
            new Color(0f, 0f, 0f, 0f)
        );

        backHitbox.raycastTarget = true;

        backButton = backRect.gameObject.AddComponent<MenuButton>();

        RectTransform backLabelRect = UIHelper.CreateStretchRect(
            "Label",
            backRect,
            Vector2.zero,
            Vector2.zero
        );

        Text backLabel = UIHelper.AddText(
            backLabelRect,
            config.chapterBackTextStyle,
            config.chapterBackLabel
        );

        backLabel.raycastTarget = false;

        backButton.Configure(
            backLabel,
            null,
            backHitbox,
            config.chapterBackStyle,
            audioPlayer
        );

        backButton.Clicked += OnBackClicked;

        built = true;
        root.gameObject.SetActive(false);
    }

    /// <summary>
    /// Binds the chapter screen authored in the Canvas. The list stays
    /// data-driven: the inactive template is cloned once per chapter.
    /// </summary>
    private void BindAuthored(RectTransform parent)
    {
        root = view.root != null ? view.root : parent;
        content = view.content;
        emptyText = view.emptyText;
        backButton = view.backButton;
        chapterButtonTemplate = view.chapterButtonTemplate;

        if (view.applyConfigText)
        {
            if (view.titleText != null)
                view.titleText.text = config.chapterScreenTitle;

            if (emptyText != null)
                emptyText.text = config.chapterEmptyLabel;

            if (backButton != null)
                backButton.SetLabel(config.chapterBackLabel);
        }

        if (view.viewport != null && content != null)
        {
            ScrollRect scroll = view.viewport.GetComponent<ScrollRect>();

            if (scroll == null)
                scroll = view.viewport.gameObject.AddComponent<ScrollRect>();

            scroll.content = content;
            scroll.viewport = view.viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;

            if (content.GetComponent<VerticalLayoutGroup>() == null)
            {
                VerticalLayoutGroup layout =
                    content.gameObject.AddComponent<VerticalLayoutGroup>();

                layout.padding = new RectOffset(12, 12, 8, 8);
                layout.spacing = config.chapterButtonSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            if (content.GetComponent<ContentSizeFitter>() == null)
            {
                ContentSizeFitter fitter =
                    content.gameObject.AddComponent<ContentSizeFitter>();

                fitter.horizontalFit =
                    ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        if (backButton != null)
        {
            backButton.Bind(audioPlayer);
            backButton.Clicked += OnBackClicked;
        }

        built = true;
        root.gameObject.SetActive(false);
    }

    public void Show()
    {
        if (!built)
            return;

        if (!listBuilt)
            BuildList();

        Refresh();
        root.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (!built)
            return;

        for (int i = 0; i < chapters.Count; i++)
        {
            ChapterDefinition chapter = chapters[i];

            if (chapter == null || i >= chapterButtons.Count)
                continue;

            bool available = ChapterAvailability.IsAvailable(
                chapter,
                database
            );

            chapterButtons[i].SetInteractable(available);
            chapterButtons[i].SetLabel(
                FormatLabel(chapter, available)
            );
        }
    }

    private void BuildList()
    {
        listBuilt = true;
        chapters.Clear();
        chapterButtons.Clear();

        List<ChapterDefinition> ordered = database != null
            ? database.GetOrderedChapters()
            : new List<ChapterDefinition>();

        for (int i = 0; i < ordered.Count; i++)
        {
            ChapterDefinition chapter = ordered[i];

            if (chapter == null)
                continue;

            chapters.Add(chapter);

            MenuButton button = CreateChapterButton(chapter);
            chapterButtons.Add(button);
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(chapters.Count == 0);
    }

    private MenuButton CreateChapterButton(ChapterDefinition chapter)
    {
        if (chapterButtonTemplate != null)
        {
            GameObject instance = Instantiate(
                chapterButtonTemplate,
                content,
                false
            );

            instance.name = "Chapter_" + chapter.chapterId;
            instance.SetActive(true);

            MenuButton templateButton = instance.GetComponent<MenuButton>();

            if (templateButton == null)
                templateButton = instance.AddComponent<MenuButton>();

            templateButton.Bind(audioPlayer);
            templateButton.Clicked += () => OnChapterClicked(chapter);

            return templateButton;
        }

        RectTransform rect = UIHelper.CreateRect(
            "Chapter_" + chapter.chapterId,
            content,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            Vector2.zero,
            new Vector2(0f, config.chapterButtonHeight)
        );

        LayoutElement layout =
            rect.gameObject.AddComponent<LayoutElement>();

        layout.minHeight = config.chapterButtonHeight;
        layout.preferredHeight = config.chapterButtonHeight;
        layout.flexibleWidth = 1f;

        Image hitbox = UIHelper.AddPanel(
            rect,
            new Color(0f, 0f, 0f, 0f)
        );

        hitbox.raycastTarget = true;

        MenuButton button = rect.gameObject.AddComponent<MenuButton>();

        RectTransform labelRect = UIHelper.CreateStretchRect(
            "Label",
            rect,
            new Vector2(28f, 0f),
            new Vector2(-28f, 0f)
        );

        Text label = UIHelper.AddText(
            labelRect,
            config.chapterButtonTextStyle,
            string.Empty
        );

        label.raycastTarget = false;

        button.Configure(
            label,
            null,
            hitbox,
            config.chapterButtonStyle,
            audioPlayer
        );

        button.Clicked += () => OnChapterClicked(chapter);

        return button;
    }

    private string FormatLabel(
        ChapterDefinition chapter,
        bool available)
    {
        string label = string.Format(
            config.chapterLabelFormat,
            chapter.chapterNumber,
            chapter.chapterTitle
        );

        if (!available)
            label += config.chapterLockedSuffix;

        return label;
    }

    private void OnChapterClicked(ChapterDefinition chapter)
    {
        if (chapter == null ||
            !ChapterAvailability.IsAvailable(chapter, database))
        {
            return;
        }

        bool started = SceneFlow.GoToChapter(
            chapter,
            config.showChapterIntroOnChapterSelect,
            TransitionReason.ChapterSelect
        );

        if (!started)
        {
            string message = string.IsNullOrEmpty(SceneFlow.LastError)
                ? "Could not open " + chapter.chapterTitle + "."
                : SceneFlow.LastError;

            statusReported?.Invoke(message);
        }
    }

    private void OnBackClicked()
    {
        backRequested?.Invoke();
    }
}
