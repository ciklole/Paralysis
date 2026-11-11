using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal diegetic compass for the driving sequence. It reads the existing
/// CarDriveDestination, rotates a small chevron cluster toward it relative to
/// the active camera, and fades away when the store is reached. The arrow is
/// screen-space so it can never be lost behind geometry or clipped by the car.
/// </summary>
public class CarNavigationGuide : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to find the scene's CarDriveDestination.")]
    [SerializeField] private CarDriveDestination destination;

    [SerializeField] private Transform car;
    [SerializeField] private string carTag = "Vehicle";
    [SerializeField] private string carName = "Free Racing Car";

    [Tooltip("Objective that must still be active for the guide to show.")]
    [SerializeField] private string objectiveId = "drive_to_store";

    [Header("Timing")]
    [SerializeField] private float showDelay = 1.5f;
    [SerializeField] private float hideDistance = 16f;
    [SerializeField] private float fadeSpeed = 2.5f;

    [Header("Look")]
    [SerializeField] private string labelText = "STORE";
    [SerializeField] private Color arrowColor =
        new Color(0.72f, 1f, 0.82f, 0.9f);

    [SerializeField] private float followAngleSpeed = 7f;
    [SerializeField] private float pulseSpeed = 1.7f;
    [SerializeField] private float pulseAmount = 0.08f;

    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform arrowRoot;
    private Text label;
    private Text distanceText;
    private Sprite chevronSprite;
    private Texture2D chevronTexture;
    private float showAt;
    private float currentAngle;
    private bool warnedMissingDestination;

    public static CarNavigationGuide Ensure()
    {
        CarNavigationGuide existing =
            FindAnyObjectByType<CarNavigationGuide>();

        if (existing != null)
            return existing;

        GameObject go = new GameObject("CarNavigationGuide");
        return go.AddComponent<CarNavigationGuide>();
    }

    public CarDriveDestination Destination => destination;

    public Transform TrackedCar => car;

    public bool HasDestination => destination != null;

    public float DisplayAlpha => group != null ? group.alpha : 0f;

    public Canvas NavigationCanvas => canvas;

    public void Configure(Transform targetCar, CarDriveDestination target)
    {
        car = targetCar;
        destination = target;

        if (destination == null)
            destination = FindAnyObjectByType<CarDriveDestination>();

        showAt = Time.unscaledTime + showDelay;
        EnsureUI();
    }

    private void Awake()
    {
        EnsureUI();
    }

    private void Start()
    {
        if (destination == null)
            destination = FindAnyObjectByType<CarDriveDestination>();

        if (car == null)
            car = FindCar();

        showAt = Time.unscaledTime + showDelay;
    }

    private void EnsureUI()
    {
        if (canvas != null)
            return;

        canvas = UIHelper.CreateCanvas(
            "CarNavigationCanvas",
            180,
            false
        );

        canvas.transform.SetParent(transform, false);

        RectTransform root = UIHelper.CreateRect(
            "NavigationRoot",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -70f),
            new Vector2(280f, 200f)
        );

        group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        TextStyle labelStyle = new TextStyle
        {
            fontSize = 18,
            bold = true,
            color = new Color(
                arrowColor.r,
                arrowColor.g,
                arrowColor.b,
                0.78f
            ),
            alignment = TextAnchor.MiddleCenter
        };

        TextStyle distanceStyle = new TextStyle
        {
            fontSize = 17,
            bold = false,
            color = new Color(
                arrowColor.r,
                arrowColor.g,
                arrowColor.b,
                0.62f
            ),
            alignment = TextAnchor.MiddleCenter
        };

        RectTransform labelRect = UIHelper.CreateRect(
            "Label",
            root,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -8f),
            new Vector2(260f, 26f)
        );

        label = UIHelper.AddText(labelRect, labelStyle, labelText);
        label.raycastTarget = false;

        arrowRoot = UIHelper.CreateRect(
            "Arrow",
            root,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -54f),
            new Vector2(140f, 100f)
        );

        chevronSprite = CreateChevronSprite();

        CreateChevron(arrowRoot, "Tip", 56f, 28f, 0.95f);
        CreateChevron(arrowRoot, "Middle", 46f, 0f, 0.55f);
        CreateChevron(arrowRoot, "Tail", 36f, -24f, 0.3f);

        RectTransform distanceRect = UIHelper.CreateRect(
            "Distance",
            root,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -160f),
            new Vector2(260f, 26f)
        );

        distanceText = UIHelper.AddText(
            distanceRect,
            distanceStyle,
            string.Empty
        );

        distanceText.raycastTarget = false;
    }

    private void CreateChevron(
        RectTransform parent,
        string name,
        float size,
        float y,
        float alpha)
    {
        RectTransform rect = UIHelper.CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, y),
            new Vector2(size, size)
        );

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = chevronSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        Color color = arrowColor;
        color.a *= alpha;
        image.color = color;
    }

    private Sprite CreateChevronSprite()
    {
        const int size = 64;
        chevronTexture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        chevronTexture.name = "CarNavigationChevron";
        chevronTexture.wrapMode = TextureWrapMode.Clamp;
        chevronTexture.filterMode = FilterMode.Bilinear;

        Vector2[] points =
        {
            new Vector2(14f, 18f),
            new Vector2(32f, 40f),
            new Vector2(50f, 18f)
        };

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pixel = new Vector2(x + 0.5f, y + 0.5f);

                float distance = Mathf.Min(
                    DistanceToSegment(pixel, points[0], points[1]),
                    DistanceToSegment(pixel, points[1], points[2])
                );

                // A crisp core with a soft halo. This keeps the chevron
                // readable against the dark road without a hard HUD outline.
                float alpha = 1f - Mathf.SmoothStep(2.5f, 8f, distance);
                alpha *= 0.85f;

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        chevronTexture.SetPixels(pixels);
        chevronTexture.Apply();

        return Sprite.Create(
            chevronTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private static float DistanceToSegment(
        Vector2 point,
        Vector2 start,
        Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;

        if (lengthSquared <= 0.0001f)
            return Vector2.Distance(point, start);

        float t = Mathf.Clamp01(
            Vector2.Dot(point - start, segment) / lengthSquared
        );

        return Vector2.Distance(point, start + segment * t);
    }

    private void LateUpdate()
    {
        if (canvas == null)
            return;

        if (destination == null)
            destination = FindAnyObjectByType<CarDriveDestination>();

        if (car == null)
            car = FindCar();

        if (destination == null)
        {
            if (!warnedMissingDestination)
            {
                warnedMissingDestination = true;

                Debug.LogWarning(
                    "CarNavigationGuide: no CarDriveDestination in the " +
                    "scene. Navigation arrows stay hidden.",
                    this
                );
            }

            FadeTo(0f);
            return;
        }

        Transform view = ResolveView();

        if (view == null)
        {
            FadeTo(0f);
            return;
        }

        Vector3 targetPosition = destination.DestinationPosition;
        Vector3 toTarget = targetPosition - view.position;
        toTarget.y = 0f;

        Vector3 forward = view.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f && car != null)
        {
            forward = car.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        float distance = toTarget.magnitude;
        bool objectiveDone = ObjectiveManager.Instance != null &&
                             ObjectiveManager.Instance.IsDone(objectiveId);

        bool shouldShow =
            !destination.HasTriggered &&
            !objectiveDone &&
            distance > hideDistance &&
            Time.unscaledTime >= showAt &&
            !UIState.BlocksGameplay;

        if (shouldShow && toTarget.sqrMagnitude > 0.001f)
        {
            float targetAngle = Vector3.SignedAngle(
                forward.normalized,
                toTarget.normalized,
                Vector3.up
            );

            currentAngle = Mathf.LerpAngle(
                currentAngle,
                targetAngle,
                Time.unscaledDeltaTime * followAngleSpeed
            );

            arrowRoot.localRotation = Quaternion.Euler(
                0f,
                0f,
                -currentAngle
            );
        }

        if (distanceText != null)
        {
            distanceText.text = distance >= 1000f
                ? (distance / 1000f).ToString("F1") + " KM"
                : Mathf.RoundToInt(distance) + " M";
        }

        FadeTo(shouldShow ? 1f : 0f);
    }

    private Transform ResolveView()
    {
        Camera main = Camera.main;

        if (main != null)
            return main.transform;

        CarCamera carCamera = FindAnyObjectByType<CarCamera>();

        if (carCamera != null)
            return carCamera.transform;

        return car != null ? car : null;
    }

    private Transform FindCar()
    {
        if (car != null)
            return car;

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

    private void FadeTo(float target)
    {
        if (group == null)
            return;

        float pulse = 1f;

        if (target > 0f)
        {
            pulse = 1f - pulseAmount +
                    pulseAmount * Mathf.Sin(
                        Time.unscaledTime * pulseSpeed
                    );
        }

        float destinationAlpha = target * pulse;

        group.alpha = Mathf.MoveTowards(
            group.alpha,
            destinationAlpha,
            Time.unscaledDeltaTime * fadeSpeed
        );
    }

    private void OnDestroy()
    {
        if (chevronSprite != null)
            Destroy(chevronSprite);

        if (chevronTexture != null)
            Destroy(chevronTexture);
    }
}
