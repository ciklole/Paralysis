using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small centre-screen message that hides itself. Used for instructions like
/// "Press M to view your shopping list". It never blocks input or movement.
/// </summary>
public class ToastUI : MonoBehaviour
{
    public static ToastUI Instance;

    [Header("Layout")]
    [SerializeField] private Vector2 screenAnchor = new Vector2(0.5f, 0.44f);
    [SerializeField] private Vector2 panelSize = new Vector2(1180f, 90f);
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.78f);
    [SerializeField] private float padding = 18f;

    [Header("Text")]
    [SerializeField] private TextStyle style = new TextStyle
    {
        fontSize = 26,
        bold = false,
        color = Color.white
    };

    [Header("Authored Canvas")]
    [Tooltip(
        "Prefab with a ToastView. Leave empty to load " +
        "Resources/Paralysis/ToastCanvas.")]
    [SerializeField] private GameObject canvasPrefab;

    [Tooltip("Turn off to force the runtime-built fallback toast.")]
    [SerializeField] private bool useAuthoredCanvas = true;

    private const string AuthoredCanvasPath = "Paralysis/ToastCanvas";

    private ToastView view;
    private Canvas canvas;
    private RectTransform panel;
    private Text text;
    private float hideAt = -1f;

    public static ToastUI Ensure()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("ToastUI");
        return go.AddComponent<ToastUI>();
    }

    /// <summary>Shows a message for the given number of seconds (0 = until hidden).</summary>
    public static void Show(string message, float duration)
    {
        Ensure().ShowMessage(message, duration);
    }

    public static void HideCurrent()
    {
        if (Instance != null)
            Instance.HideMessage();
    }

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
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void CreateUI()
    {
        if (TryCreateAuthoredUI())
            return;

        canvas = UIHelper.CreateCanvas("ToastCanvas", 500, false);
        canvas.transform.SetParent(transform, false);

        panel = UIHelper.CreateRect(
            "ToastPanel",
            canvas.transform,
            screenAnchor,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            panelSize
        );

        UIHelper.AddPanel(panel, panelColor);
        UIHelper.AddMask(panel);

        RectTransform textRect = UIHelper.CreateStretchRect(
            "ToastText",
            panel,
            new Vector2(padding, 6f),
            new Vector2(-padding, -6f)
        );

        text = UIHelper.AddText(textRect, style, string.Empty);
        text.verticalOverflow = VerticalWrapMode.Truncate;

        panel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Uses the editable ToastCanvas prefab when one is available. The prefab
    /// is parented to this persistent manager so it is cleaned up with the
    /// manager.
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
        instance.name = "ToastCanvas";

        view = instance.GetComponentInChildren<ToastView>(true);

        if (view == null || view.panel == null || view.text == null)
        {
            Debug.LogWarning(
                "ToastUI: the authored ToastCanvas is missing its panel or " +
                "text. Falling back to the runtime toast.",
                this
            );

            view = null;
            Destroy(instance);
            return false;
        }

        canvas = view.canvas != null
            ? view.canvas
            : instance.GetComponent<Canvas>();

        panel = view.panel;
        text = view.text;

        if (view.applyStyles)
        {
            style.Apply(text);
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        if (view.panelGroup != null)
        {
            view.panelGroup.alpha = 1f;
            view.panelGroup.blocksRaycasts = false;
            view.panelGroup.interactable = false;
        }

        panel.gameObject.SetActive(false);
        return true;
    }

    public void ShowMessage(string message, float duration)
    {
        if (string.IsNullOrEmpty(message))
            return;

        text.text = message;
        panel.gameObject.SetActive(true);

        hideAt = duration > 0f
            ? Time.unscaledTime + duration
            : -1f;
    }

    public void HideMessage()
    {
        hideAt = -1f;

        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (hideAt < 0f)
            return;

        if (Time.unscaledTime < hideAt)
            return;

        HideMessage();
    }
}
