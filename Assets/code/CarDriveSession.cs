using System.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Remembers the player's spot in the original scene while they are driving.
/// On exit, the driving scene reloads the original scene and this class puts
/// the player back where they were standing when they got in.
/// </summary>
[AutoStaticsCleanup]
public static partial class CarDriveSession
{
    public static bool Active { get; private set; }
    public static bool HasReturnPoint { get; private set; }
    public static string ReturnScene { get; private set; } = string.Empty;

    public static Vector3 ReturnPlayerPosition { get; private set; }
    public static Quaternion ReturnPlayerRotation { get; private set; }

    public static Vector3 ReturnCarPosition { get; private set; }
    public static Quaternion ReturnCarRotation { get; private set; }

    /// <summary>
    /// Call from the original scene just before loading the driving scene.
    /// </summary>
    public static void Begin(
        string returnScene,
        Transform car,
        Transform player)
    {
        Active = true;
        HasReturnPoint = true;

        ReturnScene = string.IsNullOrEmpty(returnScene)
            ? SceneManager.GetActiveScene().name
            : returnScene;

        if (player != null)
        {
            ReturnPlayerPosition = player.position;
            ReturnPlayerRotation = player.rotation;
        }
        else
        {
            ReturnPlayerPosition = car != null
                ? car.position + car.right * 2.5f
                : Vector3.zero;

            ReturnPlayerRotation = car != null
                ? car.rotation
                : Quaternion.identity;
        }

        if (car != null)
        {
            ReturnCarPosition = car.position;
            ReturnCarRotation = car.rotation;
        }
    }

    /// <summary>
    /// Used when the driving scene is played directly. There is no stored
    /// return point, but the exit key still knows which scene to fall back to.
    /// </summary>
    public static void MarkActive()
    {
        Active = true;
    }

    /// <summary>
    /// Direct-play fallback: remember the car so exiting still puts the player
    /// beside it in the fallback scene.
    /// </summary>
    public static void MarkActive(Transform car)
    {
        Active = true;

        if (car == null)
            return;

        ReturnCarPosition = car.position;
        ReturnCarRotation = car.rotation;

        if (HasReturnPoint)
            return;

        ReturnPlayerPosition = car.position + car.right * 2.5f;
        ReturnPlayerRotation = car.rotation;
        HasReturnPoint = true;
    }

    /// <summary>
    /// Finds the Rigidbody that owns the car, even when the tagged object is a
    /// child collider. The controller and camera must use the Rigidbody root.
    /// </summary>
    public static Transform FindDriveRoot(Transform car)
    {
        if (car == null)
            return null;

        Rigidbody body = car.GetComponent<Rigidbody>();

        if (body == null)
            body = car.GetComponentInParent<Rigidbody>();

        if (body == null)
            body = car.GetComponentInChildren<Rigidbody>();

        return body != null ? body.transform : car;
    }

    /// <summary>
    /// Called when the player presses E. The car is returned to the same
    /// world pose it had in the driving scene, and the player is placed
    /// beside it.
    /// </summary>
    public static void SetExitPose(Transform car)
    {
        if (car == null)
            return;

        ReturnCarPosition = car.position;
        ReturnCarRotation = car.rotation;
        ReturnPlayerPosition = car.position + car.right * 2.5f;
        ReturnPlayerRotation = car.rotation;
        HasReturnPoint = true;
    }

    /// <summary>
    /// Loads the stored scene and queues the player placement. Returns false
    /// when the scene cannot be loaded, so the caller can keep driving.
    /// </summary>
    public static bool ReturnToOriginalScene(string fallbackScene = "")
    {
        string scene = ReturnScene;

        if (string.IsNullOrEmpty(scene))
            scene = fallbackScene;

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning(
                "CarDriveSession: no return scene set. Enter the driving " +
                "scene through CarInteraction, or set a fallback scene."
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogWarning(
                "CarDriveSession: return scene '" + scene + "' is not in " +
                "Build Settings."
            );

            return false;
        }

        GameObject host = new GameObject("CarDriveReturn");
        Object.DontDestroyOnLoad(host);

        CarDriveReturnApplier applier =
            host.AddComponent<CarDriveReturnApplier>();

        applier.Configure(
            scene,
            ReturnPlayerPosition,
            ReturnPlayerRotation
        );

        SceneManager.LoadScene(scene);
        return true;
    }

    public static void Clear()
    {
        Active = false;
        HasReturnPoint = false;
        ReturnScene = string.Empty;
    }
}

/// <summary>
/// One-shot helper that survives the scene load and puts the player back at
/// the stored position.
/// </summary>
public class CarDriveReturnApplier : MonoBehaviour
{
    private string sceneName;
    private Vector3 position;
    private Quaternion rotation;
    private bool applied;

    public void Configure(
        string returnScene,
        Vector3 playerPosition,
        Quaternion playerRotation)
    {
        sceneName = returnScene;
        position = playerPosition;
        rotation = playerRotation;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (applied || scene.name != sceneName)
            return;

        applied = true;
        StartCoroutine(ApplyNextFrame());
    }

    private IEnumerator ApplyNextFrame()
    {
        // Let the scene's own player Awake/Start and spawn code run first.
        yield return null;

        Transform player = TeleportSequence.FindScenePlayer("Player");

        if (player == null)
            player = InteractionUtil.FindPlayer();

        if (CarDriveSession.HasReturnPoint)
        {
            Transform car = FindReturnCar();

            if (car != null)
            {
                Rigidbody carBody = car.GetComponent<Rigidbody>();

                if (carBody == null)
                    carBody = car.GetComponentInChildren<Rigidbody>();

                if (carBody != null)
                {
                    carBody.linearVelocity = Vector3.zero;
                    carBody.angularVelocity = Vector3.zero;
                    carBody.position = CarDriveSession.ReturnCarPosition;
                    carBody.rotation = CarDriveSession.ReturnCarRotation;
                    carBody.WakeUp();
                }

                car.position = CarDriveSession.ReturnCarPosition;
                car.rotation = CarDriveSession.ReturnCarRotation;
            }
        }

        if (player != null)
        {
            Rigidbody body = player.GetComponent<Rigidbody>();

            if (body == null)
                body = player.GetComponentInChildren<Rigidbody>();

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = position;
                body.rotation = rotation;
            }

            CharacterController controller =
                player.GetComponent<CharacterController>();

            if (controller != null)
                controller.enabled = false;

            player.position = position;
            player.rotation = rotation;

            if (controller != null)
                controller.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Debug.LogWarning(
                "CarDriveReturnApplier: no player found in '" +
                sceneName + "'."
            );
        }

        if (QuestLogUI.Instance != null)
            QuestLogUI.Instance.Close();

        PlayerControl.ForceUnlockAll();

        if (PlayerInteractor.Instance != null)
            PlayerInteractor.Instance.SetSuspended(false);

        CarDriveSession.Clear();
        Destroy(gameObject);
    }

    private static Transform FindReturnCar()
    {
        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Vehicle");

            if (tagged != null)
                return CarDriveSession.FindDriveRoot(tagged.transform);
        }
        catch (UnityException)
        {
            // The tag is not defined; fall back to the interaction component.
        }

        CarInteraction interaction =
            Object.FindAnyObjectByType<CarInteraction>();

        return interaction != null
            ? CarDriveSession.FindDriveRoot(interaction.transform.root)
            : null;
    }
}
