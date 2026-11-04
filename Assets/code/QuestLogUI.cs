using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The M-key mission log: objectives plus the shopping list with quantities.
/// One panel is built once and reused; it always reads live state.
/// </summary>
public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI Instance;

    [Header("Key")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    [SerializeField] private bool lockPlayerWhileOpen = true;

    [Header("Automatic Opening")]
    [Tooltip("Optional extra trigger. Leave empty to let StoryDirector control it.")]
    [SerializeField] private string autoOpenScene = "";

    [Header("Text")]
    [SerializeField] private string titleText = "MISSION LOG";

    [SerializeField] private TextStyle titleStyle = new TextStyle
    {
        fontSize = 40,
        color = new Color(1f, 0.84f, 0.42f)
    };

    [SerializeField] private TextStyle bodyStyle = new TextStyle
    {
        fontSize = 26,
        bold = false,
        alignment = TextAnchor.UpperLeft
    };

    [Header("Layout")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color panelColor = new Color(0.06f, 0.06f, 0.08f, 0.97f);
    [SerializeField] private Vector2 panelSize = new Vector2(880f, 700f);
    [SerializeField] private float padding = 40f;
    [SerializeField] private float refreshInterval = 0.2f;

    [Header("Authored Canvas")]
    [Tooltip(
        "Prefab with a QuestLogView. Leave empty to load " +
        "Resources/Paralysis/QuestLogCanvas.")]
    [SerializeField] private GameObject canvasPrefab;

    [Tooltip("Turn off to force the runtime-built fallback mission log.")]
    [SerializeField] private bool useAuthoredCanvas = true;

    private const string AuthoredCanvasPath = "Paralysis/QuestLogCanvas";

    private QuestLogView view;
    private Canvas canvas;
    private Text bodyText;
    private ScrollRect scrollRect;
    private bool open;
    private bool autoOpenQueued;
    private float nextRefresh;

    public static QuestLogUI Ensure()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("QuestLogUI");
        return go.AddComponent<QuestLogUI>();
    }

    public bool IsOpen => open;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy is deferred to the end of the frame. Deactivate first so
            // the duplicate cannot create UI, subscribe, or leave a modal/lock
            // behind during that frame.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateUI();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (open)
            Close();

        if (!string.IsNullOrEmpty(autoOpenScene) && scene.name == autoOpenScene)
            QueueAutoOpen();
    }

    private void CreateUI()
    {
        if (TryCreateAuthoredUI())
            return;

        canvas = UIHelper.CreateCanvas("QuestLogCanvas", 400, false);
        canvas.transform.SetParent(transform, false);

        RectTransform dim = UIHelper.CreateStretchRect(
            "Dim",
            canvas.transform,
            Vector2.zero,
            Vector2.zero
        );

        UIHelper.AddPanel(dim, dimColor);

        RectTransform panel = UIHelper.CreateRect(
            "QuestPanel",
            canvas.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            panelSize
        );

        UIHelper.AddPanel(panel, panelColor);

        RectTransform titleRect = UIHelper.CreateRect(
            "Title",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(padding, -padding),
            new Vector2(panelSize.x - padding * 2f, 56f)
        );

        Text title = UIHelper.AddText(titleRect, titleStyle, titleText);
        title.alignment = TextAnchor.UpperLeft;

        RectTransform viewport = UIHelper.CreateRect(
            "Viewport",
            panel,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(padding, padding),
            new Vector2(
                panelSize.x - padding * 2f,
                panelSize.y - padding * 2f - 80f
            )
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

        bodyText = contentObject.AddComponent<Text>();
        bodyText.text = string.Empty;
        bodyStyle.Apply(bodyText);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        canvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Uses the editable QuestLogCanvas prefab when one is available. The
    /// prefab is parented to this persistent manager so it is cleaned up with
    /// the manager.
    /// </summary>
    private bool TryCreateAuthoredUI()
    {
        UIHelper.EnsureEventSystem();

        GameObject prefab = canvasPrefab;

        if (prefab == null && useAuthoredCanvas)
            prefab = Resources.Load<GameObject>(AuthoredCanvasPath);

        if (prefab == null)
            return false;

        GameObject instance = Instantiate(prefab, transform, false);
        instance.name = "QuestLogCanvas";

        view = instance.GetComponentInChildren<QuestLogView>(true);

        if (view == null || view.bodyText == null)
        {
            Debug.LogWarning(
                "QuestLogUI: the authored QuestLogCanvas is missing its body " +
                "text. Falling back to the runtime mission log.",
                this
            );

            view = null;
            Destroy(instance);
            return false;
        }

        canvas = view.canvas != null
            ? view.canvas
            : instance.GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning(
                "QuestLogUI: the authored QuestLogCanvas has no Canvas. " +
                "Falling back to the runtime mission log.",
                this
            );

            view = null;
            Destroy(instance);
            return false;
        }

        bodyText = view.bodyText;
        scrollRect = view.scrollRect;

        if (view.applyStyles)
        {
            if (view.titleText != null)
                titleStyle.Apply(view.titleText);

            bodyStyle.Apply(bodyText);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (view.titleText != null &&
            string.IsNullOrEmpty(view.titleText.text))
        {
            view.titleText.text = titleText;
        }

        canvas.gameObject.SetActive(false);
        return true;
    }

    private void Update()
    {
        if (autoOpenQueued && !open && !UIState.BlocksGameplay)
        {
            autoOpenQueued = false;
            Open();
        }

        if (Input.GetKeyDown(toggleKey) &&
            (UIState.Top == null || UIState.Top == this))
        {
            if (open)
            {
                Close();
            }
            else if (!UIState.BlocksGameplay)
            {
                Open();
            }
        }

        if (!open)
            return;

        if (Time.unscaledTime < nextRefresh)
            return;

        nextRefresh = Time.unscaledTime + refreshInterval;
        Refresh();
    }

    public void Toggle()
    {
        if (open)
            Close();
        else
            Open();
    }

    /// <summary>Waits until no dialogue or inspection owns the screen.</summary>
    public void QueueAutoOpen()
    {
        if (open)
            return;

        autoOpenQueued = true;
    }

    public void Open()
    {
        if (open)
            return;

        open = true;
        autoOpenQueued = false;

        Refresh();

        canvas.gameObject.SetActive(true);
        UIState.PushModal(this);

        if (lockPlayerWhileOpen)
            PlayerControl.Lock(this);

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        // Also cancel a pending auto-open, so closing from a scene that
        // should not show the log cannot be undone a frame later.
        autoOpenQueued = false;

        if (!open)
            return;

        open = false;
        canvas.gameObject.SetActive(false);

        UIState.PopModal(this);
        PlayerControl.Unlock(this);
    }

    private void Refresh()
    {
        bodyText.text = BuildContent();
    }

    private string BuildContent()
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("<b>OBJECTIVES</b>");
        builder.AppendLine();

        bool anyObjective = false;

        if (ObjectiveManager.Instance != null)
        {
            IReadOnlyList<ObjectiveEntry> entries =
                ObjectiveManager.Instance.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                ObjectiveEntry entry = entries[i];

                if (entry == null)
                    continue;

                anyObjective = true;

                if (entry.done)
                {
                    builder.AppendLine(
                        "<color=#8f8f8f>[x] " + entry.text + "</color>"
                    );
                }
                else
                {
                    builder.AppendLine("- " + entry.text);
                }
            }
        }

        if (!anyObjective)
            builder.AppendLine("<color=#8f8f8f>No objectives yet.</color>");

        builder.AppendLine();
        builder.AppendLine("<b>SHOPPING LIST</b>");
        builder.AppendLine();

        ShoppingCart cart = ShoppingCart.Instance;

        if (cart != null && cart.Items != null && cart.Items.Count > 0)
        {
            List<ShoppingItem> sorted = new List<ShoppingItem>(cart.Items);
            sorted.Sort((a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;
                return a.displayOrder.CompareTo(b.displayOrder);
            });

            for (int i = 0; i < sorted.Count; i++)
            {
                ShoppingItem item = sorted[i];

                if (item == null)
                    continue;

                string line =
                    item.displayName + "   " +
                    Mathf.Min(item.collectedQuantity, item.requiredQuantity) +
                    " / " + item.requiredQuantity;

                if (item.IsComplete)
                {
                    builder.AppendLine(
                        "<color=#8f8f8f>[x] " + line + "</color>"
                    );
                }
                else
                {
                    builder.AppendLine("[ ] " + line);
                }
            }

            builder.AppendLine();

            if (cart.IsComplete)
            {
                builder.AppendLine(
                    "<color=#8f8f8f><b>PROGRESS: " +
                    cart.TotalCollected + " / " + cart.TotalRequired +
                    "  -  SHOPPING COMPLETE</b></color>"
                );
            }
            else
            {
                builder.AppendLine(
                    "<b>PROGRESS: " + cart.TotalCollected +
                    " / " + cart.TotalRequired + "</b>"
                );
            }
        }
        else
        {
            builder.AppendLine(
                "<color=#8f8f8f>Nothing on the list.</color>"
            );
        }

        return builder.ToString();
    }
}
