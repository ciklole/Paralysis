
using UnityEngine;
using UnityEngine.UI;

public class TeleportOnKey : MonoBehaviour
{
    public enum ScreenPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [Header("Destination")]
    public string destinationScene;
    public string playerTag = "Player";

    [Header("Spawn")]
    [Tooltip("NamedSpawnPoint: finds a GameObject in the new scene.\nExactCoordinates: teleports to the exact X / Y / Z below.")]
    public Teleport_Scenes.SpawnMode spawnMode =
        Teleport_Scenes.SpawnMode.NamedSpawnPoint;

    [Tooltip("Name of the spawn GameObject to look for (NamedSpawnPoint mode).")]
    public string spawnPointName = "City";

    [Tooltip("Where the player lands, in world space (ExactCoordinates mode).")]
    public Vector3 spawnPosition = Vector3.zero;

    [Tooltip("Rotation applied to the player, as Euler angles (ExactCoordinates mode).")]
    public Vector3 spawnEulerRotation = Vector3.zero;

    [Tooltip("Size of the player after the teleport. 1, 1, 1 = unchanged.")]
    public Vector3 playerScale = Vector3.one;

    [Tooltip("Log a warning when the named spawn point is missing.")]
    public bool warnIfSpawnPointMissing = true;

    [Header("Interaction")]
    public KeyCode interactionKey = KeyCode.E;
    public float interactionDistance = 5f;
    public Transform interactionPoint;
    public Transform playerInteractionPoint;

    [Header("Interaction UI")]
    public string promptMessage = "Press E to enter the vehicle.";
    public Font promptFont;
    public int promptFontSize = 24;
    public bool promptBold = true;
    public Color promptColor = Color.white;
    public ScreenPosition promptPosition = ScreenPosition.BottomRight;
    public Vector2 promptOffset = new Vector2(-40f, 40f);
    public Vector2 promptSize = new Vector2(700f, 80f);
    public float promptScreenMargin = 0f;
    public TextAnchor promptAlignment = TextAnchor.MiddleRight;
    public bool showPromptBackground = false;
    public Color promptBackgroundColor =
        new Color(0f, 0f, 0f, 0.65f);

    [Header("Loading Screen")]
    public string loadingMessage = "Loading";
    public Font loadingFont;
    public int loadingFontSize = 30;
    public bool loadingBold = true;
    public Color loadingTextColor = Color.white;
    public ScreenPosition loadingPosition =
        ScreenPosition.MiddleCenter;
    public Vector2 loadingOffset = Vector2.zero;
    public float loadingScreenMargin = 30f;
    public bool animateDots = true;
    public float dotInterval = 0.4f;
    public float fadeDuration = 0.5f;
    public float loadingDuration = 0.5f;

    private Transform player;

    private Canvas promptCanvas;
    private Text promptText;
    private Image promptBackground;

    private bool playerInRange;
    private bool isLoading;

    private void Start()
    {
        FindPlayer();
        CreatePromptUI();
    }

    private void Update()
    {
        if (isLoading)
            return;

        if (UIState.BlocksGameplay)
        {
            SetPromptVisible(false);
            return;
        }

        if (player == null)
            FindPlayer();

        if (player == null)
        {
            SetPromptVisible(false);
            return;
        }

        Vector3 targetPosition = interactionPoint != null
            ? interactionPoint.position
            : transform.position;

        Vector3 playerPosition = playerInteractionPoint != null
            ? playerInteractionPoint.position
            : player.position;

        float distance = Vector3.Distance(
            targetPosition,
            playerPosition
        );

        playerInRange = distance <= interactionDistance;

        SetPromptVisible(playerInRange);

        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError(
                    "Teleport failed. Check destinationScene and " +
                    "make sure the scene is included in Build Profiles."
                );
                return;
            }

            isLoading = true;

            SetPromptVisible(false);

            // The whole sequence - loading screen, scene change, which player
            // to use and the spawn - runs on a persistent object. This trigger
            // is a child inside a prefab instance, so a coroutine started here
            // would be killed by the scene load and the screen would freeze.
            TeleportSequence.Begin(BuildConfig(), player, OnTeleportFailed);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(
            playerTag
        );

        if (playerObject == null)
            return;

        player = playerObject.transform;
    }

    private void CreatePromptUI()
    {
        GameObject canvasObject = new GameObject(
            "InteractionPromptCanvas"
        );

        promptCanvas = canvasObject.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 10;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject(
            "PromptBackground"
        );

        backgroundObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform backgroundRect =
            backgroundObject.AddComponent<RectTransform>();

        ConfigureRect(
            backgroundRect,
            promptPosition,
            promptOffset,
            promptSize,
            promptScreenMargin
        );

        promptBackground = backgroundObject.AddComponent<Image>();
        promptBackground.color = promptBackgroundColor;
        promptBackground.raycastTarget = false;

        promptBackground.enabled = showPromptBackground;

        GameObject textObject = new GameObject("PromptText");

        textObject.transform.SetParent(
            backgroundObject.transform,
            false
        );

        RectTransform textRect =
            textObject.AddComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 5f);
        textRect.offsetMax = new Vector2(-10f, -5f);

        promptText = textObject.AddComponent<Text>();

        promptText.font = promptFont != null
            ? promptFont
            : GameFont.Default;

        promptText.text = promptMessage;
        promptText.fontSize = promptFontSize;

        promptText.fontStyle = promptBold
            ? FontStyle.Bold
            : FontStyle.Normal;

        promptText.color = promptColor;
        promptText.alignment = promptAlignment;
        promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        promptText.verticalOverflow = VerticalWrapMode.Overflow;
        promptText.raycastTarget = false;

        SetPromptVisible(false);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptCanvas != null)
            promptCanvas.enabled = visible;
    }

    private TeleportConfig BuildConfig()
    {
        return new TeleportConfig
        {
            destinationScene = destinationScene,
            playerTag = playerTag,
            spawnMode = spawnMode,
            spawnPointName = spawnPointName,
            spawnPosition = spawnPosition,
            spawnEulerRotation = spawnEulerRotation,
            playerScale = playerScale,
            warnIfSpawnPointMissing = warnIfSpawnPointMissing,
            loadingMessage = loadingMessage,
            loadingFont = loadingFont,
            fontSize = loadingFontSize,
            bold = loadingBold,
            textColor = loadingTextColor,
            textPosition = (Teleport_Scenes.ScreenPosition)(int)loadingPosition,
            textOffset = loadingOffset,
            screenMargin = loadingScreenMargin,
            animateDots = animateDots,
            dotInterval = dotInterval,
            fadeDuration = fadeDuration,
            loadingDuration = loadingDuration
        };
    }

    /// <summary>
    /// Called by the loader when the scene change fails, so the prompt
    /// comes back instead of the teleport silently dying.
    /// </summary>
    private void OnTeleportFailed()
    {
        isLoading = false;
        SetPromptVisible(true);
    }

    private void ConfigureRect(
        RectTransform rect,
        ScreenPosition position,
        Vector2 offset,
        Vector2 size,
        float margin)
    {
        Vector2 anchor = Vector2.zero;

        switch (position)
        {
            case ScreenPosition.TopLeft:
                anchor = new Vector2(0f, 1f);
                break;

            case ScreenPosition.TopCenter:
                anchor = new Vector2(0.5f, 1f);
                break;

            case ScreenPosition.TopRight:
                anchor = new Vector2(1f, 1f);
                break;

            case ScreenPosition.MiddleLeft:
                anchor = new Vector2(0f, 0.5f);
                break;

            case ScreenPosition.MiddleCenter:
                anchor = new Vector2(0.5f, 0.5f);
                break;

            case ScreenPosition.MiddleRight:
                anchor = new Vector2(1f, 0.5f);
                break;

            case ScreenPosition.BottomLeft:
                anchor = new Vector2(0f, 0f);
                break;

            case ScreenPosition.BottomCenter:
                anchor = new Vector2(0.5f, 0f);
                break;

            case ScreenPosition.BottomRight:
                anchor = new Vector2(1f, 0f);
                break;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;

        float x = offset.x;
        float y = offset.y;

        if (anchor.x == 0f)
            x += margin;
        else if (anchor.x == 1f)
            x -= margin;

        if (anchor.y == 0f)
            y += margin;
        else if (anchor.y == 1f)
            y -= margin;

        rect.anchoredPosition = new Vector2(x, y);
    }

    private TextAnchor GetTextAlignment(ScreenPosition position)
    {
        switch (position)
        {
            case ScreenPosition.TopLeft:
                return TextAnchor.UpperLeft;

            case ScreenPosition.TopCenter:
                return TextAnchor.UpperCenter;

            case ScreenPosition.TopRight:
                return TextAnchor.UpperRight;

            case ScreenPosition.MiddleLeft:
                return TextAnchor.MiddleLeft;

            case ScreenPosition.MiddleCenter:
                return TextAnchor.MiddleCenter;

            case ScreenPosition.MiddleRight:
                return TextAnchor.MiddleRight;

            case ScreenPosition.BottomLeft:
                return TextAnchor.LowerLeft;

            case ScreenPosition.BottomCenter:
                return TextAnchor.LowerCenter;

            case ScreenPosition.BottomRight:
                return TextAnchor.LowerRight;

            default:
                return TextAnchor.MiddleCenter;
        }
    }

}
