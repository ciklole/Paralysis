using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Arrival marker for the driving scene. When the car reaches the marker the
/// scene transitions to the store and lets StoryDirector run its normal store
/// arrival sequence. All positions, distances, text and timings are editable.
/// </summary>
public class CarDriveDestination : MonoBehaviour
{
    [Header("Car")]
    [SerializeField] private Transform carOverride;
    [SerializeField] private string carTag = "Vehicle";
    [SerializeField] private string carName = "Free Racing Car";

    [Header("Destination")]
    [Tooltip("Leave empty to use StoryDirector's store scene.")]
    [SerializeField] private string destinationScene = "";

    [Tooltip("World position of the arrival zone. Zero uses this object's position.")]
    [SerializeField] private Vector3 destinationPosition = Vector3.zero;

    [SerializeField] private float triggerRadius = 9f;
    [SerializeField] private float verticalTolerance = 6f;

    [Header("Feedback")]
    [SerializeField] private string arrivalMessage = "You made it. Head inside.";
    [SerializeField] private float arrivalMessageDuration = 2.5f;
    [SerializeField] private float loadDelay = 0.75f;

    [Header("Events")]
    [SerializeField] private UnityEvent onArrived;

    private bool triggered;

    /// <summary>Diagnostic state for the smoke test and scene tools.</summary>
    public string LastStage { get; private set; } = "idle";

    public bool HasTriggered => triggered;

    public Transform WatchedCar => FindCar();

    /// <summary>
    /// World position of the arrival zone. The root transform is authoritative,
    /// so moving the "Store Destination" object in the scene moves the trigger
    /// point and every marker child together.
    /// </summary>
    public Vector3 DestinationPosition => transform.position;

    private void Start()
    {
        // Keep the serialized value in sync for older scenes and inspector
        // tooling. The transform is what the trigger actually uses.
        destinationPosition = transform.position;

        // Make the visible marker match the trigger point even if a child was
        // moved by hand in an older version of the scene.
        SnapMarkerChildren();
    }

    private void SnapMarkerChildren()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child == null)
                continue;

            if (child.name.IndexOf(
                    "Marker",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                child.localPosition = new Vector3(0f, 0.06f, 0f);
            }
            else if (child.name.IndexOf(
                    "Beam",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                child.localPosition = new Vector3(0f, 4f, 0f);
            }
            else if (child.name.IndexOf(
                    "Sign",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                child.localPosition = new Vector3(0f, 3.2f, 0f);
            }
        }
    }

    private void Update()
    {
        if (triggered)
            return;

        Transform car = FindCar();

        if (car == null)
            return;

        Vector3 delta = car.position - DestinationPosition;

        if (new Vector2(delta.x, delta.z).magnitude > triggerRadius)
            return;

        if (Mathf.Abs(delta.y) > verticalTolerance)
            return;

        StartCoroutine(Arrive(car));
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
                    return CarDriveSession.FindDriveRoot(tagged.transform);
            }
            catch (UnityException)
            {
                // Tag is not defined in this project; use the name instead.
            }
        }

        if (!string.IsNullOrEmpty(carName))
        {
            GameObject named = GameObject.Find(carName);

            if (named != null)
                return CarDriveSession.FindDriveRoot(named.transform);
        }

        CarDriveController controller =
            FindAnyObjectByType<CarDriveController>();

        return controller != null ? controller.transform : null;
    }

    private IEnumerator Arrive(Transform car)
    {
        triggered = true;
        LastStage = "arrive";

        string scene = destinationScene;

        if (string.IsNullOrEmpty(scene) && StoryDirector.Instance != null)
            scene = StoryDirector.Instance.StoreScene;

        if (string.IsNullOrEmpty(scene) ||
            !Application.CanStreamedLevelBeLoaded(scene))
        {
            LastStage = "invalid scene '" + scene + "'";

            Debug.LogWarning(
                "CarDriveDestination: destination scene '" + scene +
                "' is empty or not in Build Settings.",
                this
            );

            triggered = false;
            yield break;
        }

        LastStage = "arrived, waiting to load '" + scene + "'";

        Debug.Log(
            "CarDriveDestination: arrived at " +
            Time.realtimeSinceStartup.ToString("F2") +
            " (frame " + Time.frameCount + "), waiting " +
            loadDelay.ToString("F2") + "s."
        );

        onArrived?.Invoke();

        // Optional arrival sting. If no clip is assigned, the driving theme
        // continues until the store scene takes over.
        MusicManager.PlayCue(MusicCueIds.CarArrival);

        if (!string.IsNullOrEmpty(arrivalMessage))
            ToastUI.Show(arrivalMessage, arrivalMessageDuration);

        Rigidbody body = car.GetComponent<Rigidbody>();

        if (body == null)
            body = car.GetComponentInChildren<Rigidbody>();

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        CarDriveController controller = car.GetComponent<CarDriveController>();

        if (controller != null)
            controller.enabled = false;

        // The store scene must run its normal StoryDirector routine, so the
        // driving session cannot keep StoryDirector in its skip branch.
        if (StoryDirector.Instance != null)
            StoryDirector.Instance.NotifyCarTripStarted();

        CarDriveSession.Clear();

        // The driving scene suspends the walking interaction component. The
        // store has a walking player again, so restore it before the load.
        if (PlayerInteractor.Instance != null)
            PlayerInteractor.Instance.SetSuspended(false);

        if (loadDelay > 0f)
            yield return new WaitForSecondsRealtime(loadDelay);

        Debug.Log(
            "CarDriveDestination: wait finished at " +
            Time.realtimeSinceStartup.ToString("F2") +
            " (frame " + Time.frameCount + ")."
        );

        LastStage = "loading '" + scene + "'";
        Debug.Log("CarDriveDestination: loading '" + scene + "'.");

        SceneManager.LoadScene(scene);
    }
}
