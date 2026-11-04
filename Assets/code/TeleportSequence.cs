
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Everything a teleport needs to know. Teleport_Scenes and TeleportOnKey build
/// one of these from their own Inspector fields and hand it to Begin().
/// </summary>
public class TeleportConfig
{
    public string destinationScene;
    public string playerTag = "Player";

    public Teleport_Scenes.SpawnMode spawnMode =
        Teleport_Scenes.SpawnMode.NamedSpawnPoint;

    public string spawnPointName;
    public Vector3 spawnPosition = Vector3.zero;
    public Vector3 spawnEulerRotation = Vector3.zero;
    public Vector3 playerScale = Vector3.one;
    public bool warnIfSpawnPointMissing = true;

    public string loadingMessage = "Loading";
    public Font loadingFont;
    public int fontSize = 30;
    public bool bold = true;
    public Color textColor = Color.white;
    public Teleport_Scenes.ScreenPosition textPosition =
        Teleport_Scenes.ScreenPosition.MiddleCenter;

    public Vector2 textOffset = Vector2.zero;
    public float screenMargin = 30f;

    public bool animateDots = true;
    public float dotInterval = 0.4f;
    public float fadeDuration = 0.5f;
    public float loadingDuration = 0.5f;
}

/// <summary>
/// Runs the whole teleport on its own DontDestroyOnLoad object.
///
/// The trigger that fired the teleport lives in the scene being unloaded, so a
/// coroutine started there is killed halfway through by the scene change and the
/// loading screen freezes forever. This object outlives the load, so the fades,
/// the dots and the cleanup all survive it.
///
/// It also decides WHICH player gets teleported: if the scene we are loading
/// already contains a player of its own, that one is used and the player that
/// came along is dropped with its own scene.
/// </summary>
public class TeleportSequence : MonoBehaviour
{
    private TeleportConfig config;
    private Transform carriedPlayer;
    private Rigidbody carriedRigidbody;
    private System.Action onFailed;

    private Image background;
    private Text loadingText;
    private Coroutine dotsCoroutine;

    #region Player helpers (shared by both teleport scripts)

    /// <summary>
    /// The player that owns the collider that entered the trigger: either the
    /// tagged object itself, or the closest ancestor carrying the tag. Falls
    /// back to the root so a collider on a child still finds its player.
    /// </summary>
    public static Transform FindPlayer(Collider other, string playerTag)
    {
        Transform current = other.transform;

        while (current != null)
        {
            if (current.gameObject.tag == playerTag)
                return current;

            current = current.parent;
        }

        return other.transform.root;
    }

    /// <summary>
    /// The player that already lives in the scene we just loaded, if there is
    /// one. Players carried over by DontDestroyOnLoad are ignored, because they
    /// sit in the persistent scene instead of the freshly loaded one.
    /// </summary>
    public static Transform FindScenePlayer(string playerTag)
    {
        Scene active = SceneManager.GetActiveScene();

        if (!active.IsValid())
            return null;

        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include
        );

        foreach (Transform candidate in all)
        {
            if (candidate.gameObject.scene != active)
                continue;

            if (candidate.gameObject.tag == playerTag)
                return candidate;
        }

        return null;
    }

    #endregion

    public static TeleportSequence Begin(
        TeleportConfig teleportConfig,
        Transform player,
        System.Action failedCallback = null)
    {
        GameObject host = new GameObject("TeleportLoadingScreen");

        // The whole sequence lives on this object, so it survives the load.
        DontDestroyOnLoad(host);

        TeleportSequence sequence = host.AddComponent<TeleportSequence>();

        sequence.config = teleportConfig;
        sequence.onFailed = failedCallback;

        sequence.CarryPlayer(player);
        sequence.CreateLoadingScreen();

        sequence.StartCoroutine(sequence.Run());

        return sequence;
    }

    private void CarryPlayer(Transform player)
    {
        carriedPlayer = player;

        if (carriedPlayer == null)
            return;

        carriedRigidbody = carriedPlayer.GetComponent<Rigidbody>();

        if (carriedRigidbody == null)
            carriedRigidbody =
                carriedPlayer.GetComponentInChildren<Rigidbody>();

        StopRigidbody(carriedRigidbody);

        // Keep it alive only as a fallback for scenes that have no player of
        // their own. It is left switched on for the whole sequence so its
        // camera keeps rendering and Unity never reports that nothing is
        // being drawn - the black loading screen covers it anyway.
        DontDestroyOnLoad(carriedPlayer.gameObject);
    }

    private IEnumerator Run()
    {
        if (config.animateDots)
            dotsCoroutine = StartCoroutine(AnimateLoadingDots());

        yield return StartCoroutine(FadeBackground(0f, 1f));

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(config.destinationScene);

        if (operation == null)
        {
            Debug.LogError(
                "Failed to load scene: " + config.destinationScene
            );

            if (carriedPlayer != null)
                carriedPlayer.gameObject.SetActive(true);

            if (onFailed != null)
                onFailed();

            Finish();
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        Transform target = ResolveTargetPlayer();

        if (target != null)
        {
            target.gameObject.SetActive(true);
            ApplySpawn(target);
            target.localScale = config.playerScale;

            Rigidbody body = target.GetComponent<Rigidbody>();

            if (body == null)
                body = target.GetComponentInChildren<Rigidbody>();

            StopRigidbody(body);
        }
        else
        {
            Debug.LogWarning(
                "No player tagged '" + config.playerTag +
                "' was found in scene '" + config.destinationScene +
                "' and no player travelled with the teleport."
            );
        }

        yield return new WaitForSecondsRealtime(config.loadingDuration);

        if (dotsCoroutine != null)
            StopCoroutine(dotsCoroutine);

        yield return StartCoroutine(FadeBackground(1f, 0f));

        Finish();
    }

    /// <summary>
    /// Picks the player to teleport: the one that already belongs to the new
    /// scene, or - when the new scene has none - the one we carried over.
    /// </summary>
    private Transform ResolveTargetPlayer()
    {
        Transform scenePlayer = FindScenePlayer(config.playerTag);

        if (scenePlayer == null)
            return carriedPlayer;

        if (carriedPlayer != null)
        {
            // The new scene already has a player, so the one that came with us
            // is dropped: it stays on the scene it came from.
            Destroy(carriedPlayer.gameObject);
            carriedPlayer = null;
            carriedRigidbody = null;
        }

        return scenePlayer;
    }

    private void ApplySpawn(Transform target)
    {
        if (config.spawnMode == Teleport_Scenes.SpawnMode.ExactCoordinates)
        {
            target.SetPositionAndRotation(
                config.spawnPosition,
                Quaternion.Euler(config.spawnEulerRotation)
            );
            return;
        }

        if (string.IsNullOrEmpty(config.spawnPointName))
        {
            if (config.warnIfSpawnPointMissing)
                Debug.LogWarning(
                    "No spawn point name and not using exact coordinates, " +
                    "the player keeps the position it loaded at."
                );
            return;
        }

        GameObject spawnPoint = GameObject.Find(config.spawnPointName);

        if (spawnPoint != null)
        {
            target.SetPositionAndRotation(
                spawnPoint.transform.position,
                spawnPoint.transform.rotation
            );
        }
        else if (config.warnIfSpawnPointMissing)
        {
            Debug.LogWarning(
                "Spawn point '" + config.spawnPointName +
                "' was not found in scene '" +
                config.destinationScene + "'."
            );
        }
    }

    private static void StopRigidbody(Rigidbody body)
    {
        if (body == null)
            return;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    #region Loading screen UI

    private void CreateLoadingScreen()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("BlackBackground");
        backgroundObject.transform.SetParent(transform, false);

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
        textObject.transform.SetParent(transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();

        SetTextPosition(textRect);

        loadingText = textObject.AddComponent<Text>();

        loadingText.text = config.loadingMessage;
        loadingText.font = config.loadingFont != null
            ? config.loadingFont
            : GameFont.Default;

        loadingText.fontSize = config.fontSize;
        loadingText.fontStyle = config.bold
            ? FontStyle.Bold
            : FontStyle.Normal;

        loadingText.color = config.textColor;
        loadingText.alignment = GetTextAlignment();
        loadingText.horizontalOverflow = HorizontalWrapMode.Overflow;
        loadingText.verticalOverflow = VerticalWrapMode.Overflow;
        loadingText.raycastTarget = false;
    }

    private void SetTextPosition(RectTransform rect)
    {
        Vector2 anchor = Vector2.zero;
        Vector2 pivot = Vector2.zero;

        switch (config.textPosition)
        {
            case Teleport_Scenes.ScreenPosition.TopLeft:
                anchor = new Vector2(0, 1);
                pivot = new Vector2(0, 1);
                break;

            case Teleport_Scenes.ScreenPosition.TopCenter:
                anchor = new Vector2(0.5f, 1);
                pivot = new Vector2(0.5f, 1);
                break;

            case Teleport_Scenes.ScreenPosition.TopRight:
                anchor = new Vector2(1, 1);
                pivot = new Vector2(1, 1);
                break;

            case Teleport_Scenes.ScreenPosition.MiddleLeft:
                anchor = new Vector2(0, 0.5f);
                pivot = new Vector2(0, 0.5f);
                break;

            case Teleport_Scenes.ScreenPosition.MiddleCenter:
                anchor = new Vector2(0.5f, 0.5f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case Teleport_Scenes.ScreenPosition.MiddleRight:
                anchor = new Vector2(1, 0.5f);
                pivot = new Vector2(1, 0.5f);
                break;

            case Teleport_Scenes.ScreenPosition.BottomLeft:
                anchor = new Vector2(0, 0);
                pivot = new Vector2(0, 0);
                break;

            case Teleport_Scenes.ScreenPosition.BottomCenter:
                anchor = new Vector2(0.5f, 0);
                pivot = new Vector2(0.5f, 0);
                break;

            case Teleport_Scenes.ScreenPosition.BottomRight:
                anchor = new Vector2(1, 0);
                pivot = new Vector2(1, 0);
                break;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = new Vector2(800, 100);
        rect.anchoredPosition = config.textOffset;

        if (anchor.x == 0)
            rect.anchoredPosition += new Vector2(config.screenMargin, 0);
        else if (anchor.x == 1)
            rect.anchoredPosition -= new Vector2(config.screenMargin, 0);

        if (anchor.y == 0)
            rect.anchoredPosition += new Vector2(0, config.screenMargin);
        else if (anchor.y == 1)
            rect.anchoredPosition -= new Vector2(0, config.screenMargin);
    }

    private TextAnchor GetTextAlignment()
    {
        switch (config.textPosition)
        {
            case Teleport_Scenes.ScreenPosition.TopLeft:
                return TextAnchor.UpperLeft;

            case Teleport_Scenes.ScreenPosition.TopCenter:
                return TextAnchor.UpperCenter;

            case Teleport_Scenes.ScreenPosition.TopRight:
                return TextAnchor.UpperRight;

            case Teleport_Scenes.ScreenPosition.MiddleLeft:
                return TextAnchor.MiddleLeft;

            case Teleport_Scenes.ScreenPosition.MiddleCenter:
                return TextAnchor.MiddleCenter;

            case Teleport_Scenes.ScreenPosition.MiddleRight:
                return TextAnchor.MiddleRight;

            case Teleport_Scenes.ScreenPosition.BottomLeft:
                return TextAnchor.LowerLeft;

            case Teleport_Scenes.ScreenPosition.BottomCenter:
                return TextAnchor.LowerCenter;

            case Teleport_Scenes.ScreenPosition.BottomRight:
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
                    config.loadingMessage + new string('.', dots);
            }

            dots = (dots + 1) % 4;

            yield return new WaitForSecondsRealtime(config.dotInterval);
        }
    }

    private IEnumerator FadeBackground(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = background.color;
        color.a = startAlpha;
        background.color = color;

        if (config.fadeDuration <= 0f)
        {
            color.a = endAlpha;
            background.color = color;
            yield break;
        }

        while (elapsed < config.fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            color.a = Mathf.Lerp(
                startAlpha,
                endAlpha,
                elapsed / config.fadeDuration
            );

            background.color = color;

            yield return null;
        }

        color.a = endAlpha;
        background.color = color;
    }

    #endregion

    private void Finish()
    {
        if (dotsCoroutine != null)
            StopCoroutine(dotsCoroutine);

        // The canvas is this object, so it goes away with it.
        Destroy(gameObject);
    }
}
