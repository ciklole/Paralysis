
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Teleport_Scenes : MonoBehaviour
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
    public string spawnPointName = "PlayerSpawn";

    [Header("Loading Screen")]
    public string loadingMessage = "Loading";
    public Font loadingFont;
    public int fontSize = 30;
    public bool bold = true;
    public Color textColor = Color.white;
    public ScreenPosition textPosition = ScreenPosition.MiddleCenter;
    public Vector2 textOffset = Vector2.zero;
    public float screenMargin = 30f;

    [Header("Loading Animation")]
    public bool animateDots = true;
    public float dotInterval = 0.4f;
    public float fadeDuration = 0.5f;
    public float loadingDuration = 0.5f;

    private bool isLoading;
    private Canvas loadingCanvas;
    private Image background;
    private Text loadingText;
    private Coroutine dotsCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading || !other.CompareTag(playerTag))
            return;

        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError(
                "Teleport failed: destination scene '" +
                destinationScene +
                "' is not in the Build Profiles scene list."
            );
            return;
        }

        Transform player = other.transform.root;

        isLoading = true;

        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        StartCoroutine(TeleportPlayer(player));
    }

    private IEnumerator TeleportPlayer(Transform player)
    {
        // Preserve the player between scenes.
        DontDestroyOnLoad(player.gameObject);

        Rigidbody playerRigidbody =
            player.GetComponent<Rigidbody>();

        if (playerRigidbody == null)
            playerRigidbody = player.GetComponentInChildren<Rigidbody>();

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        CreateLoadingScreen();

        if (animateDots)
            dotsCoroutine = StartCoroutine(AnimateLoadingDots());

        yield return StartCoroutine(FadeBackground(0f, 1f));

        // Load the destination scene.
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(destinationScene);

        if (operation == null)
        {
            Debug.LogError("Failed to load scene: " + destinationScene);
            DestroyLoadingScreen();
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        // Find the destination spawn point.
        GameObject spawnPoint = GameObject.Find(spawnPointName);

        if (spawnPoint != null)
        {
            player.position = spawnPoint.transform.position;
            player.rotation = spawnPoint.transform.rotation;
        }
        else
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

        DestroyLoadingScreen();
    }

    private void CreateLoadingScreen()
    {
        GameObject canvasObject =
            new GameObject("TeleportLoadingCanvas");

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

        GameObject backgroundObject =
            new GameObject("BlackBackground");

        backgroundObject.transform.SetParent(
            canvasObject.transform, false);

        RectTransform backgroundRect =
            backgroundObject.AddComponent<RectTransform>();

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0f);
        background.raycastTarget = true;

        GameObject textObject = new GameObject("LoadingText");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect =
            textObject.AddComponent<RectTransform>();

        SetTextPosition(textRect);

        loadingText = textObject.AddComponent<Text>();

        loadingText.text = loadingMessage;
        loadingText.font = loadingFont != null
            ? loadingFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        loadingText.fontSize = fontSize;
        loadingText.fontStyle = bold
            ? FontStyle.Bold
            : FontStyle.Normal;

        loadingText.color = textColor;
        loadingText.alignment = GetTextAlignment();
        loadingText.horizontalOverflow = HorizontalWrapMode.Overflow;
        loadingText.verticalOverflow = VerticalWrapMode.Overflow;
        loadingText.raycastTarget = false;
    }

    private void SetTextPosition(RectTransform rect)
    {
        Vector2 anchor = Vector2.zero;
        Vector2 pivot = Vector2.zero;

        switch (textPosition)
        {
            case ScreenPosition.TopLeft:
                anchor = new Vector2(0, 1);
                pivot = new Vector2(0, 1);
                break;

            case ScreenPosition.TopCenter:
                anchor = new Vector2(0.5f, 1);
                pivot = new Vector2(0.5f, 1);
                break;

            case ScreenPosition.TopRight:
                anchor = new Vector2(1, 1);
                pivot = new Vector2(1, 1);
                break;

            case ScreenPosition.MiddleLeft:
                anchor = new Vector2(0, 0.5f);
                pivot = new Vector2(0, 0.5f);
                break;

            case ScreenPosition.MiddleCenter:
                anchor = new Vector2(0.5f, 0.5f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case ScreenPosition.MiddleRight:
                anchor = new Vector2(1, 0.5f);
                pivot = new Vector2(1, 0.5f);
                break;

            case ScreenPosition.BottomLeft:
                anchor = new Vector2(0, 0);
                pivot = new Vector2(0, 0);
                break;

            case ScreenPosition.BottomCenter:
                anchor = new Vector2(0.5f, 0);
                pivot = new Vector2(0.5f, 0);
                break;

            case ScreenPosition.BottomRight:
                anchor = new Vector2(1, 0);
                pivot = new Vector2(1, 0);
                break;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = textOffset;
        rect.sizeDelta = new Vector2(800, 100);

        if (anchor.x == 0)
            rect.anchoredPosition += new Vector2(screenMargin, 0);
        else if (anchor.x == 1)
            rect.anchoredPosition -= new Vector2(screenMargin, 0);

        if (anchor.y == 0)
            rect.anchoredPosition += new Vector2(0, screenMargin);
        else if (anchor.y == 1)
            rect.anchoredPosition -= new Vector2(0, screenMargin);
    }

    private TextAnchor GetTextAlignment()
    {
        switch (textPosition)
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
        float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = background.color;
        color.a = startAlpha;
        background.color = color;

        if (fadeDuration <= 0f)
        {
            color.a = endAlpha;
            background.color = color;
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
            background.color = color;

            yield return null;
        }

        color.a = endAlpha;
        background.color = color;
    }

    private void DestroyLoadingScreen()
    {
        if (dotsCoroutine != null)
            StopCoroutine(dotsCoroutine);

        if (loadingCanvas != null)
            Destroy(loadingCanvas.gameObject);
    }
}
