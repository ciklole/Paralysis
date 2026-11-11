using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal "keep this resolution?" prompt. It lives on a persistent object so a
/// scene load or closing the settings menu cannot strand the player in an
/// unusable display mode. If the prompt is ignored it reverts automatically.
/// </summary>
public class ResolutionGuard : MonoBehaviour
{
    private static ResolutionGuard instance;
    private static bool pending;
    private static int previousWidth;
    private static int previousHeight;
    private static int pendingWidth;
    private static int pendingHeight;
    private static float deadline;

    private Canvas canvas;
    private GameObject panel;
    private Text countdownText;
    private bool built;

    public static bool Pending => pending;

    public static int PreviousWidth => previousWidth;
    public static int PreviousHeight => previousHeight;

    public static int PendingWidth => pendingWidth;
    public static int PendingHeight => pendingHeight;

    public static float RemainingSeconds =>
        pending ? Mathf.Max(0f, deadline - Time.unscaledTime) : 0f;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        pending = false;
        previousWidth = 0;
        previousHeight = 0;
        pendingWidth = 0;
        pendingHeight = 0;
        deadline = 0f;
    }

    public static void Begin(
        int oldWidth,
        int oldHeight,
        int newWidth,
        int newHeight,
        float timeout)
    {
        EnsureInstance();

        // A second request while a prompt is open should not stack prompts.
        if (pending)
        {
            pending = false;
            instance.HidePanel();

            GameSettings.RevertResolution(
                previousWidth,
                previousHeight
            );
        }

        previousWidth = oldWidth;
        previousHeight = oldHeight;
        pendingWidth = newWidth;
        pendingHeight = newHeight;
        deadline = Time.unscaledTime + Mathf.Max(5f, timeout);
        pending = true;

        instance.ShowPanel();
    }

    public static void Confirm()
    {
        if (!pending || instance == null)
            return;

        pending = false;
        instance.HidePanel();
        GameSettings.CommitResolution(pendingWidth, pendingHeight);
    }

    public static void Revert()
    {
        if (!pending || instance == null)
            return;

        pending = false;
        instance.HidePanel();

        GameSettings.RevertResolution(previousWidth, previousHeight);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("ResolutionGuard");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ResolutionGuard>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        UIState.PopModal(this);
    }

    private void BuildUI()
    {
        if (built)
            return;

        built = true;

        canvas = UIHelper.CreateCanvas(
            "ResolutionConfirmCanvas",
            800,
            false
        );

        canvas.transform.SetParent(transform, false);

        RectTransform backdrop = UIHelper.CreateStretchRect(
            "Backdrop",
            canvas.transform,
            Vector2.zero,
            Vector2.zero
        );

        Image backdropImage = UIHelper.AddPanel(
            backdrop,
            new Color(0f, 0f, 0f, 0.55f)
        );

        backdropImage.raycastTarget = true;

        RectTransform panelRect = UIHelper.CreateRect(
            "Panel",
            canvas.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(780f, 310f)
        );

        UIHelper.AddPanel(panelRect, new Color(0.03f, 0.03f, 0.03f, 0.96f));

        TextStyle titleStyle = new TextStyle
        {
            fontSize = 34,
            bold = true,
            color = Color.white,
            alignment = TextAnchor.MiddleCenter
        };

        TextStyle bodyStyle = new TextStyle
        {
            fontSize = 22,
            bold = false,
            color = new Color(0.72f, 0.72f, 0.72f, 1f),
            alignment = TextAnchor.MiddleCenter
        };

        TextStyle countdownStyle = new TextStyle
        {
            fontSize = 24,
            bold = true,
            color = new Color(0.85f, 0.82f, 0.65f, 1f),
            alignment = TextAnchor.MiddleCenter
        };

        RectTransform titleRect = UIHelper.CreateRect(
            "Title",
            panelRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -42f),
            new Vector2(700f, 54f)
        );

        UIHelper.AddText(titleRect, titleStyle, "KEEP THIS RESOLUTION?");

        RectTransform bodyRect = UIHelper.CreateRect(
            "Body",
            panelRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -104f),
            new Vector2(700f, 42f)
        );

        UIHelper.AddText(
            bodyRect,
            bodyStyle,
            "If you cannot see this screen clearly, revert."
        );

        RectTransform countdownRect = UIHelper.CreateRect(
            "Countdown",
            panelRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -154f),
            new Vector2(700f, 38f)
        );

        countdownText = UIHelper.AddText(
            countdownRect,
            countdownStyle,
            string.Empty
        );

        RectTransform buttons = UIHelper.CreateRect(
            "Buttons",
            panelRect,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 46f),
            new Vector2(640f, 62f)
        );

        HorizontalLayoutGroup layout =
            buttons.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = 48f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        MenuButton keepButton = CreateButton(
            buttons,
            "Keep",
            "KEEP",
            220f
        );

        keepButton.Clicked += Confirm;

        MenuButton revertButton = CreateButton(
            buttons,
            "Revert",
            "REVERT",
            220f
        );

        revertButton.Clicked += Revert;

        panel = panelRect.gameObject;
        panel.SetActive(false);
        canvas.gameObject.SetActive(false);
    }

    private MenuButton CreateButton(
        Transform parent,
        string name,
        string label,
        float width)
    {
        RectTransform rect = UIHelper.CreateRect(
            name,
            parent,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(width, 56f)
        );

        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = 56f;
        layout.preferredHeight = 56f;

        Image hitbox = UIHelper.AddPanel(
            rect,
            new Color(1f, 1f, 1f, 0.035f)
        );

        hitbox.raycastTarget = true;

        MenuButton button = rect.gameObject.AddComponent<MenuButton>();

        RectTransform labelRect = UIHelper.CreateStretchRect(
            "Label",
            rect,
            new Vector2(8f, 2f),
            new Vector2(-8f, -2f)
        );

        TextStyle style = new TextStyle
        {
            fontSize = 28,
            bold = false,
            color = new Color(0.72f, 0.72f, 0.72f, 1f),
            alignment = TextAnchor.MiddleCenter
        };

        Text text = UIHelper.AddText(labelRect, style, label);

        MenuButtonStyle buttonStyle = new MenuButtonStyle
        {
            hoverOffset = 5f,
            duration = 0.12f,
            normalFontSize = 28,
            hoverFontSize = 31,
            normalColor = new Color(0.72f, 0.72f, 0.72f, 1f),
            hoverColor = Color.white,
            normalBold = false,
            hoverBold = true
        };

        button.Configure(text, null, hitbox, buttonStyle, null);

        return button;
    }

    private void ShowPanel()
    {
        if (!built)
            BuildUI();

        if (canvas != null)
            canvas.gameObject.SetActive(true);

        if (panel != null)
            panel.SetActive(true);

        UIState.PushModal(this);
    }

    private void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);

        if (canvas != null)
            canvas.gameObject.SetActive(false);

        UIState.PopModal(this);
    }

    private void Update()
    {
        if (!pending)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Revert();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Confirm();
            return;
        }

        float remaining = deadline - Time.unscaledTime;

        if (remaining <= 0f)
        {
            Revert();
            return;
        }

        if (countdownText != null)
        {
            countdownText.text =
                "Reverting in " + Mathf.CeilToInt(remaining) + " seconds";
        }
    }
}
