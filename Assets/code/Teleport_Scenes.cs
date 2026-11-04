
using UnityEngine;

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

    public enum SpawnMode
    {
        NamedSpawnPoint,
        ExactCoordinates
    }

    [Header("Destination")]
    public string destinationScene;
    public string playerTag = "Player";

    [Header("Spawn")]
    [Tooltip("NamedSpawnPoint: finds a GameObject in the new scene.\nExactCoordinates: teleports to the exact X / Y / Z below.")]
    public SpawnMode spawnMode = SpawnMode.NamedSpawnPoint;

    [Tooltip("Name of the spawn GameObject to look for (NamedSpawnPoint mode).")]
    public string spawnPointName = "PlayerSpawn";

    [Tooltip("Where the player lands, in world space (ExactCoordinates mode).")]
    public Vector3 spawnPosition = Vector3.zero;

    [Tooltip("Rotation applied to the player, as Euler angles (ExactCoordinates mode).")]
    public Vector3 spawnEulerRotation = Vector3.zero;

    [Tooltip("Size of the player after the teleport. 1, 1, 1 = unchanged.")]
    public Vector3 playerScale = Vector3.one;

    [Tooltip("Log a warning when the named spawn point is missing.")]
    public bool warnIfSpawnPointMissing = true;

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

        isLoading = true;

        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        // Hand the whole sequence over to a persistent object. This component
        // is destroyed with the current scene, so a coroutine started here
        // would be killed by the scene load and the loading screen would
        // freeze forever.
        TeleportSequence.Begin(
            BuildConfig(),
            TeleportSequence.FindPlayer(other, playerTag),
            ResetTrigger
        );
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
            fontSize = fontSize,
            bold = bold,
            textColor = textColor,
            textPosition = textPosition,
            textOffset = textOffset,
            screenMargin = screenMargin,
            animateDots = animateDots,
            dotInterval = dotInterval,
            fadeDuration = fadeDuration,
            loadingDuration = loadingDuration
        };
    }

    /// <summary>
    /// Called by the loader if the scene load fails, so the trigger can be used again.
    /// </summary>
    internal void ResetTrigger()
    {
        isLoading = false;

        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.enabled = true;
    }
}
