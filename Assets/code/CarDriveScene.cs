using UnityEngine;

/// <summary>
/// Bootstraps a copied city scene for driving. It finds the car, adds the
/// wheel controller and third-person camera, disables the walking player and
/// shows the current objective. Everything is configurable in the Inspector.
/// </summary>
public class CarDriveScene : MonoBehaviour
{
    [Header("Car")]
    [Tooltip("Optional explicit car. Leave empty to find it by tag or name.")]
    [SerializeField] private Transform carOverride;

    [SerializeField] private string carName = "Free Racing Car";
    [SerializeField] private string carTag = "Vehicle";

    [Header("Objective")]
    [SerializeField] private bool setObjectiveOnStart = true;
    [SerializeField] private string objectiveId = "drive_to_store";
    [SerializeField] private string objectiveText = "Drive to the store";

    [Header("Hint")]
    [SerializeField]
    private string controlsHint =
        "W/S drive  -  A/D steer  -  Mouse look  -  E get out";

    [SerializeField] private float hintDuration = 6f;

    [Header("Return")]
    [Tooltip("Used when the driving scene is played directly.")]
    [SerializeField] private string returnFallbackScene = "City";

    [Header("Camera")]
    [Tooltip("Third person orbits behind the car. First person sits at the driver's eye point.")]
    [SerializeField] private CarCameraViewMode cameraView =
        CarCameraViewMode.ThirdPerson;

    [Tooltip("Use the shared mouse-sensitivity setting while driving.")]
    [SerializeField] private bool usePlayerMouseSensitivity = true;

    private void Awake()
    {
        // Awake runs before the sceneLoaded callback. If the driving scene is
        // played directly, this lets StoryDirector skip its normal scene
        // routine instead of waiting for a walking player we are about to
        // disable.
        if (FindCar() != null)
            CarDriveSession.MarkActive();
    }

    private void Start()
    {
        Transform car = FindCar();

        if (car == null)
        {
            Debug.LogWarning(
                "CarDriveScene: no car found. Set Car Override, or give " +
                "the car the configured tag/name.",
                this
            );

            return;
        }

        Transform driveRoot = CarDriveSession.FindDriveRoot(car);

        if (driveRoot == null)
            driveRoot = car;

        PlayerControl.ForceUnlockAll();

        // A quest log configured to auto-open on scene load would cover the
        // road and block driving input.
        if (QuestLogUI.Instance != null)
            QuestLogUI.Instance.Close();

        DisableWalkingPlayer();
        SuspendWalkingInteractions();
        DisableConflictingSystems(driveRoot);

        CarDriveController controller =
            driveRoot.GetComponent<CarDriveController>();

        if (controller == null)
            controller = driveRoot.gameObject.AddComponent<CarDriveController>();

        controller.SetReturnFallbackScene(ResolveReturnScene());

        CreateCamera(driveRoot);
        SetObjective();

        // The store scene uses this flag to know the drive objective was
        // reached by car, not by walking into the store.
        if (StoryDirector.Instance != null)
            StoryDirector.Instance.NotifyCarTripStarted();

        CarDriveSession.MarkActive(driveRoot);

        if (!string.IsNullOrEmpty(controlsHint))
            ToastUI.Show(controlsHint, hintDuration);
    }

    private string ResolveReturnScene()
    {
        // StoryDirector owns the scene roles; the serialized fallback is only
        // used when there is no director in the scene.
        if (StoryDirector.Instance != null &&
            !string.IsNullOrEmpty(StoryDirector.Instance.CityScene))
        {
            return StoryDirector.Instance.CityScene;
        }

        return returnFallbackScene;
    }

    private Transform FindCar()
    {
        if (carOverride != null)
            return carOverride;

        if (!string.IsNullOrEmpty(carTag))
        {
            try
            {
                GameObject tagged = GameObject.FindGameObjectWithTag(carTag);

                if (tagged != null)
                    return tagged.transform;
            }
            catch (UnityException)
            {
                // The tag is not defined in this project; try the name.
            }
        }

        if (!string.IsNullOrEmpty(carName))
        {
            GameObject named = GameObject.Find(carName);

            if (named != null)
                return named.transform;
        }

        CarDriveController existing =
            FindAnyObjectByType<CarDriveController>();

        return existing != null ? existing.transform : null;
    }

    private void DisableWalkingPlayer()
    {
        FirstPersonController[] players =
            FindObjectsByType<FirstPersonController>();

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            // The controller can sit on a child of the player prefab. Disable
            // the whole player so its Rigidbody and camera cannot keep
            // running beside the car.
            Transform root = players[i].transform.root;

            if (root != null)
                root.gameObject.SetActive(false);
            else
                players[i].gameObject.SetActive(false);
        }
    }

    private void SuspendWalkingInteractions()
    {
        if (PlayerInteractor.Instance != null)
            PlayerInteractor.Instance.SetSuspended(true);
    }

    private void DisableConflictingSystems(Transform car)
    {
        CarInteraction[] interactions =
            car.GetComponentsInChildren<CarInteraction>(true);

        for (int i = 0; i < interactions.Length; i++)
        {
            if (interactions[i] != null)
                interactions[i].enabled = false;
        }

        TeleportOnKey[] teleports =
            FindObjectsByType<TeleportOnKey>();

        for (int i = 0; i < teleports.Length; i++)
        {
            if (teleports[i] != null)
                teleports[i].enabled = false;
        }

        // These also listen for F/E directly. The car scene owns those keys.
        LightSwitch[] lights = FindObjectsByType<LightSwitch>();

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = false;
        }

        ObjectiveTrigger[] triggers =
            FindObjectsByType<ObjectiveTrigger>();

        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null)
                triggers[i].enabled = false;
        }
    }

    private void CreateCamera(Transform car)
    {
        CarCamera existing = FindAnyObjectByType<CarCamera>();

        if (existing != null)
        {
            AudioListener[] existingListeners =
                FindObjectsByType<AudioListener>();

            for (int i = 0; i < existingListeners.Length; i++)
            {
                if (existingListeners[i] != null &&
                    existingListeners[i].gameObject != existing.gameObject)
                {
                    existingListeners[i].enabled = false;
                }
            }

            AudioListener listener =
                existing.GetComponent<AudioListener>();

            if (listener == null)
                listener = existing.gameObject.AddComponent<AudioListener>();

            listener.enabled = true;

            existing.SetTarget(car);
            existing.ConfigureView(
                cameraView,
                usePlayerMouseSensitivity
            );

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        AudioListener[] listeners =
            FindObjectsByType<AudioListener>();

        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null)
                listeners[i].enabled = false;
        }

        GameObject go = new GameObject("CarCamera");
        Camera camera = go.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 5000f;

        go.AddComponent<AudioListener>();

        CarCamera carCamera = go.AddComponent<CarCamera>();
        carCamera.SetTarget(car);
        carCamera.ConfigureView(
            cameraView,
            usePlayerMouseSensitivity
        );

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetObjective()
    {
        if (!setObjectiveOnStart ||
            string.IsNullOrEmpty(objectiveId) ||
            string.IsNullOrEmpty(objectiveText))
        {
            return;
        }

        ObjectiveManager manager = ObjectiveManager.Ensure();

        if (!manager.HasActive(objectiveId))
            manager.SetObjective(objectiveId, objectiveText);
    }
}
