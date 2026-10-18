
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public string spawnPointName = "City";

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
    private Rigidbody playerRigidbody;

    private Canvas promptCanvas;
    private Text promptText;
    private Image promptBackground;

    private Canvas loadingCanvas;
    private Image loadingBackground;
    private Text loadingText;

    private bool playerInRange;
    private bool isLoading;
    private Coroutine dotsCoroutine;

    private void Start()
    {
        FindPlayer();
        CreatePromptUI();
    }

    private void Update()
    {
        if (isLoading)
            return;

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

            StartCoroutine(LoadDestination());
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

        playerRigidbody = player.GetComponent<Rigidbody>();

        if (playerRigidbody == null)
        {
            playerRigidbody =
                player.GetComponentInChildren<Rigidbody>();
        }
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
            : Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

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

    private IEnumerator LoadDestination()
    {
        isLoading = true;

        SetPromptVisible(false);

        // Keep this root object alive while the scene changes.
        DontDestroyOnLoad(gameObject);

        if (player != null)
            DontDestroyOnLoad(player.gameObject);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        CreateLoadingUI();

        if (animateDots)
            dotsCoroutine = StartCoroutine(AnimateLoadingDots());

        yield return StartCoroutine(FadeBackground(0f, 1f));

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(destinationScene);

        if (operation == null)
        {
            Debug.LogError(
                "Failed to load scene: " + destinationScene
            );

            if (dotsCoroutine != null)
                StopCoroutine(dotsCoroutine);

            DestroyLoadingUI();
            Destroy(gameObject);
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        GameObject spawnPoint = GameObject.Find(spawnPointName);

        if (spawnPoint != null && player != null)
        {
            player.position = spawnPoint.transform.position;
            player.rotation = spawnPoint.transform.rotation;
        }
        else if (spawnPoint == null)
        {
            Debug.LogWarning(
                "Spawn point '" + spawnPointName +
                "' was not found in scene '" + destinationScene + "'."
            );
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        yield return new WaitForSecondsRealtime(loadingDuration);

        if (dotsCoroutine != null)
            StopCoroutine(dotsCoroutine);

        yield return StartCoroutine(FadeBackground(1f, 0f));

        DestroyLoadingUI();
        Destroy(gameObject);
    }

    private void CreateLoadingUI()
    {
        GameObject canvasObject = new GameObject(
            "TeleportLoadingCanvas"
        );

        DontDestroyOnLoad(canvasObject);

        loadingCanvas = canvasObject.AddComponent<Canvas>();
        loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadingCanvas.sortingOrder = 999;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject(
            "LoadingBackground"
        );

        backgroundObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform backgroundRect =
            backgroundObject.AddComponent<RectTransform>();

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        loadingBackground = backgroundObject.AddComponent<Image>();
        loadingBackground.color = Color.black;
        loadingBackground.raycastTarget = true;

        GameObject textObject = new GameObject("LoadingText");

        textObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform textRect =
            textObject.AddComponent<RectTransform>();

        ConfigureRect(
            textRect,
            loadingPosition,
            loadingOffset,
            new Vector2(800f, 100f),
            loadingScreenMargin
        );

        loadingText = textObject.AddComponent<Text>();

        loadingText.font = loadingFont != null
            ? loadingFont
            : Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        loadingText.text = loadingMessage;
        loadingText.fontSize = loadingFontSize;

        loadingText.fontStyle = loadingBold
            ? FontStyle.Bold
            : FontStyle.Normal;

        loadingText.color = loadingTextColor;
        loadingText.alignment = GetTextAlignment(loadingPosition);
        loadingText.horizontalOverflow = HorizontalWrapMode.Overflow;
        loadingText.verticalOverflow = VerticalWrapMode.Overflow;
        loadingText.raycastTarget = false;
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

    private IEnumerator AnimateLoadingDots()
    {
        int dots = 0;

        while (true)
        {
            if (loadingText != null)
            {
                loadingText.text =
                    loadingMessage + new string('.', dots);
            }

            dots = (dots + 1) % 4;

            yield return new WaitForSecondsRealtime(dotInterval);
        }
    }

    private IEnumerator FadeBackground(
        float startAlpha,
        float endAlpha)
    {
        float elapsed = 0f;

        Color color = loadingBackground.color;
        color.a = startAlpha;
        loadingBackground.color = color;

        if (fadeDuration <= 0f)
        {
            color.a = endAlpha;
            loadingBackground.color = color;
            yield break;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                elapsed / fadeDuration
            );

            color.a = alpha;
            loadingBackground.color = color;

            yield return null;
        }

        color.a = endAlpha;
        loadingBackground.color = color;
    }

    private void DestroyLoadingUI()
    {
        if (loadingCanvas != null)
            Destroy(loadingCanvas.gameObject);

        if (promptCanvas != null)
            Destroy(promptCanvas.gameObject);
    }
}
