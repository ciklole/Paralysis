using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Teleports the player to a set location when they touch this object,
/// showing a black loading screen with animated "..." dots that slowly fades away.
/// </summary>
public class Teleport : MonoBehaviour
{
    /// <summary>Where on the screen the loading text appears.</summary>
    public enum ScreenPosition
    {
        Center,
        TopLeft,
        Top,
        TopRight,
        Left,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }

    [Header("Teleport Settings")]
    [Tooltip("World position (X, Y, Z) the player will be teleported to")]
    [SerializeField] private Vector3 teleportLocation = Vector3.zero;

    [Tooltip("Tag used to recognize the player")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("New size (X, Y, Z) the player changes to when teleporting. Keep (1, 1, 1) for normal size.")]
    [SerializeField] private Vector3 playerScale = Vector3.one;

    [Header("Loading Text")]
    [Tooltip("Text shown before the dots, e.g. 'Loading'")]
    [SerializeField] private string loadingText = "Loading";

    [Tooltip("Font for the loading text (leave empty to use the built-in font)")]
    [SerializeField] private Font loadingFont;

    [Tooltip("Size of the loading text")]
    [SerializeField] private int fontSize = 48;

    [Tooltip("Render the font in bold")]
    [SerializeField] private bool bold = true;

    [Tooltip("Color of the loading text (white looks cool on black)")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Where the loading text appears on screen (default: center)")]
    [SerializeField] private ScreenPosition screenPosition = ScreenPosition.Center;

    [Tooltip("Extra offset (in pixels) from the chosen screen position")]
    [SerializeField] private Vector2 screenOffset = Vector2.zero;

    [Tooltip("Space (in pixels) kept between the text and the screen edge")]
    [SerializeField] private float edgeMargin = 40f;

    [Tooltip("Animate the loading dots (dot 1 -> 1+2 -> 2 -> 2+3 -> 3 -> 3+1 -> repeat)")]
    [SerializeField] private bool animateDots = true;

    [Header("Loading Screen Timing")]
    [Tooltip("How long the black loading screen stays visible (seconds)")]
    [SerializeField] private float loadingDuration = 3f;

    [Tooltip("How fast the screen turns black at the start (seconds)")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("How slowly the black screen fades away at the end (seconds)")]
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Tooltip("Time between each dot animation step (seconds)")]
    [SerializeField] private float dotStepInterval = 0.25f;

    // Dot animation states. Each state shows 3 fixed slots,
    // so the dots slide through the exact pattern:
    //   1) first dot appears   2) second dot appears   3) first dot disappears
    //   4) third dot appears   5) second dot disappears 6) third disappears (after first appears)
    private static readonly string[] DotStates =
    {
        ".  ",  // dot 1 only
        ".. ",  // dots 1 + 2
        " . ",  // dot 2 only
        " ..",  // dots 2 + 3
        "  .",  // dot 3 only
        ". ."   // dots 3 + 1 -> loops back to dot 1 only
    };

    private GameObject canvasObject;
    private GameObject panelObject;
    private GameObject textObject;
    private CanvasGroup canvasGroup;
    private Text loadingLabel;
    private bool isLoading;

    private void Start()
    {
        CreateUI();
    }

    /// <summary>Builds the whole loading screen automatically at startup.</summary>
    private void CreateUI()
    {
        // Full screen overlay canvas
        canvasObject = new GameObject("TeleportLoadingCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // draw above everything else

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        // Black panel that covers the whole screen
        panelObject = new GameObject("LoadingPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image background = panelObject.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = false;
        canvasGroup = panelObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Loading text
        textObject = new GameObject("LoadingText");
        textObject.transform.SetParent(panelObject.transform, false);

        loadingLabel = textObject.AddComponent<Text>();
        loadingLabel.font = loadingFont != null ? loadingFont : GetDefaultFont();
        loadingLabel.fontSize = fontSize;
        loadingLabel.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        loadingLabel.color = textColor;
        loadingLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        loadingLabel.verticalOverflow = VerticalWrapMode.Overflow;
        loadingLabel.text = loadingText + "...";

        ApplyPosition();

        // Hidden until the player touches the object
        panelObject.SetActive(false);
    }

    private static Font GetDefaultFont()
    {
        // "LegacyRuntime.ttf" is the built-in font in modern Unity versions
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>Places the text on the chosen screen side with margin and offset.</summary>
    private void ApplyPosition()
    {
        if (textObject == null || loadingLabel == null)
            return;

        RectTransform rt = textObject.GetComponent<RectTransform>();
        Vector2 anchor;
        Vector2 pivot;
        TextAnchor alignment;
        Vector2 inwardDirection = Vector2.zero;

        switch (screenPosition)
        {
            case ScreenPosition.TopLeft:
                anchor = new Vector2(0f, 1f); pivot = anchor;
                alignment = TextAnchor.UpperLeft;
                inwardDirection = new Vector2(1f, -1f);
                break;
            case ScreenPosition.Top:
                anchor = new Vector2(0.5f, 1f); pivot = anchor;
                alignment = TextAnchor.UpperCenter;
                inwardDirection = new Vector2(0f, -1f);
                break;
            case ScreenPosition.TopRight:
                anchor = new Vector2(1f, 1f); pivot = anchor;
                alignment = TextAnchor.UpperRight;
                inwardDirection = new Vector2(-1f, -1f);
                break;
            case ScreenPosition.Left:
                anchor = new Vector2(0f, 0.5f); pivot = anchor;
                alignment = TextAnchor.MiddleLeft;
                inwardDirection = new Vector2(1f, 0f);
                break;
            case ScreenPosition.Right:
                anchor = new Vector2(1f, 0.5f); pivot = anchor;
                alignment = TextAnchor.MiddleRight;
                inwardDirection = new Vector2(-1f, 0f);
                break;
            case ScreenPosition.BottomLeft:
                anchor = new Vector2(0f, 0f); pivot = anchor;
                alignment = TextAnchor.LowerLeft;
                inwardDirection = new Vector2(1f, 1f);
                break;
            case ScreenPosition.Bottom:
                anchor = new Vector2(0.5f, 0f); pivot = anchor;
                alignment = TextAnchor.LowerCenter;
                inwardDirection = new Vector2(0f, 1f);
                break;
            case ScreenPosition.BottomRight:
                anchor = new Vector2(1f, 0f); pivot = anchor;
                alignment = TextAnchor.LowerRight;
                inwardDirection = new Vector2(-1f, 1f);
                break;
            default: // Center
                anchor = new Vector2(0.5f, 0.5f); pivot = anchor;
                alignment = TextAnchor.MiddleCenter;
                break;
        }

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = screenOffset + inwardDirection * edgeMargin;
        rt.sizeDelta = new Vector2(1600f, 200f); // text can overflow this box

        loadingLabel.alignment = alignment;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isLoading && collision.gameObject.CompareTag(playerTag))
        {
            StartTeleport(collision.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLoading && other.CompareTag(playerTag))
        {
            StartTeleport(other.transform);
        }
    }

    private void StartTeleport(Transform player)
    {
        isLoading = true; // block re-triggering while the screen is active
        StartCoroutine(LoadingRoutine(player));
    }

    private void TeleportPlayer(Transform player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        player.position = teleportLocation;

        // Change the player's size
        player.localScale = playerScale;
    }

    private IEnumerator LoadingRoutine(Transform player)
    {
        // Teleport first, while the screen is still fading to black,
        // so the player never sees the actual jump.
        TeleportPlayer(player);

        panelObject.SetActive(true);

        // 1) Fade to black
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(fadeInDuration, 0.0001f));
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2) Hold on the loading screen with animated dots
        float elapsed = 0f;
        float dotTimer = 0f;
        int dotIndex = 0;
        SetDots(dotIndex);

        while (elapsed < loadingDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            if (animateDots)
            {
                dotTimer += dt;
                if (dotTimer >= Mathf.Max(dotStepInterval, 0.05f))
                {
                    dotTimer = 0f;
                    dotIndex = (dotIndex + 1) % DotStates.Length;
                    SetDots(dotIndex);
                }
            }

            yield return null;
        }

        // 3) Slowly fade the black away
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(fadeOutDuration, 0.0001f));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panelObject.SetActive(false);
        isLoading = false; // allow the next teleport
    }

    private void SetDots(int index)
    {
        if (loadingLabel == null)
            return;

        loadingLabel.text = animateDots
            ? loadingText + DotStates[index]
            : loadingText + "...";
    }

    // Draw a marker at the teleport destination in the Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(teleportLocation, 0.5f);
    }
}
