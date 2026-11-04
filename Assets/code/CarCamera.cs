using UnityEngine;

public enum CarCameraViewMode
{
    FirstPerson,
    ThirdPerson
}

/// <summary>
/// Camera for the driving scene. The default first-person view sits at the
/// driver's eye point and uses the same saved mouse sensitivity as the walking
/// controller. Third-person orbit remains available in the Inspector.
/// </summary>
public class CarCamera : MonoBehaviour
{
    [Header("View")]
    [Tooltip("First person is the intended driving view. Third person keeps the original orbit camera.")]
    [SerializeField] private CarCameraViewMode viewMode =
        CarCameraViewMode.FirstPerson;

    [Tooltip("Use the saved mouse sensitivity from the shared settings screen.")]
    [SerializeField] private bool usePlayerMouseSensitivity = true;

    [SerializeField] private float firstPersonSensitivityScale = 1f;
    [SerializeField] private Vector3 firstPersonOffset =
        new Vector3(0f, 1.35f, 0.15f);
    [SerializeField] private float firstPersonMinPitch = -55f;
    [SerializeField] private float firstPersonMaxPitch = 55f;
    [SerializeField] private float firstPersonStartPitch = 0f;

    [Tooltip("Hide the car body while in first person so the camera is not inside the mesh.")]
    [SerializeField] private bool hideCarInFirstPerson = true;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 12f;
    [SerializeField] private float height = 4.5f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private float startPitch = 15f;

    [Header("Follow")]
    [Tooltip("How quickly the camera swings back behind the car.")]
    [SerializeField] private float followSmooth = 4f;

    [Tooltip("How quickly the mouse offset recentres when the mouse is still.")]
    [SerializeField] private float centerSpeed = 1.5f;

    [Tooltip("Maximum temporary left/right look offset in degrees.")]
    [SerializeField] private float maxYawOffset = 150f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 8f;
    [SerializeField] private float rotationSmooth = 10f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float minimumDistance = 2f;

    private float yaw;
    private float yawOffset;
    private float pitch;
    private Vector3 positionVelocity;
    private Renderer[] carRenderers;
    private bool[] carRendererStates;

    public CarCameraViewMode ViewMode => viewMode;
    public bool UsesPlayerMouseSensitivity => usePlayerMouseSensitivity;
    public Transform Target => target;
    public int LastUpdateFrame { get; private set; }

    public void SetViewMode(CarCameraViewMode mode)
    {
        viewMode = mode;
        pitch = mode == CarCameraViewMode.FirstPerson
            ? firstPersonStartPitch
            : startPitch;

        ApplyCarVisibility();
    }

    public void SetUsePlayerMouseSensitivity(bool value)
    {
        usePlayerMouseSensitivity = value;
    }

    public void ConfigureView(
        CarCameraViewMode mode,
        bool playerMouseSensitivity)
    {
        usePlayerMouseSensitivity = playerMouseSensitivity;
        SetViewMode(mode);
    }

    public void SetTarget(Transform value)
    {
        if (target != value)
        {
            carRenderers = null;
            carRendererStates = null;
        }

        target = value;
        positionVelocity = Vector3.zero;
        CacheCarRenderers();

        if (target != null)
        {
            yaw = target.eulerAngles.y;
            yawOffset = 0f;
            pitch = viewMode == CarCameraViewMode.FirstPerson
                ? firstPersonStartPitch
                : startPitch;
        }

        ApplyCarVisibility();
    }

    private void Start()
    {
        if (target == null)
        {
            CarDriveController car =
                FindAnyObjectByType<CarDriveController>();

            if (car != null)
                SetTarget(car.transform);
        }

        if (target != null)
        {
            yaw = target.eulerAngles.y;
            yawOffset = 0f;
            pitch = viewMode == CarCameraViewMode.FirstPerson
                ? firstPersonStartPitch
                : startPitch;
        }

        CacheCarRenderers();
        ApplyCarVisibility();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        LastUpdateFrame = Time.frameCount;

        if (target == null)
            return;

        if (viewMode == CarCameraViewMode.FirstPerson)
        {
            UpdateFirstPerson();
            return;
        }

        if (!UIState.BlocksGameplay)
        {
            float mouseX = Input.GetAxis("Mouse X");

            yawOffset += mouseX * mouseSensitivity;
            yawOffset = Mathf.Clamp(
                yawOffset,
                -maxYawOffset,
                maxYawOffset
            );

            pitch += Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            if (Mathf.Abs(mouseX) < 0.01f)
            {
                yawOffset = Mathf.Lerp(
                    yawOffset,
                    0f,
                    Time.deltaTime * centerSpeed
                );
            }
        }

        // Follow the car's heading, plus whatever temporary look offset the
        // mouse has added. This keeps the car on screen while steering.
        float targetYaw = target.eulerAngles.y + yawOffset;
        yaw = Mathf.LerpAngle(
            yaw,
            targetYaw,
            Time.deltaTime * followSmooth
        );

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        Vector3 desired = focus - orbit * Vector3.forward * distance;

        desired = ResolveCollision(focus, desired);

        float smoothTime = 1f / Mathf.Max(1f, positionSmooth);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref positionVelocity,
            smoothTime
        );

        Vector3 lookDirection = focus - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection, Vector3.up),
                Time.deltaTime * rotationSmooth
            );
        }
    }

    /// <summary>
    /// Eye-level view that turns with the car and the mouse. There is no
    /// orbit recentring here: in first person the player owns the look offset.
    /// </summary>
    private void UpdateFirstPerson()
    {
        if (!UIState.BlocksGameplay)
        {
            float sensitivity = usePlayerMouseSensitivity
                ? GameSettings.MouseSensitivity * firstPersonSensitivityScale
                : mouseSensitivity;

            yawOffset += Input.GetAxis("Mouse X") * sensitivity;
            yawOffset = Mathf.Clamp(
                yawOffset,
                -maxYawOffset,
                maxYawOffset
            );

            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(
                pitch,
                firstPersonMinPitch,
                firstPersonMaxPitch
            );
        }

        yaw = target.eulerAngles.y + yawOffset;

        transform.position = target.TransformPoint(firstPersonOffset);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void CacheCarRenderers()
    {
        if (target == null)
        {
            carRenderers = null;
            carRendererStates = null;
            return;
        }

        // Cache the authored visibility only once. Start() runs after
        // CreateCamera() has already hidden the body for first person; caching
        // again there would remember "hidden" as the original state and the
        // third-person view could never restore it.
        if (carRenderers != null &&
            carRendererStates != null &&
            carRenderers.Length == carRendererStates.Length)
        {
            return;
        }

        carRenderers = target.GetComponentsInChildren<Renderer>(true);
        carRendererStates = new bool[carRenderers.Length];

        for (int i = 0; i < carRenderers.Length; i++)
        {
            carRendererStates[i] =
                carRenderers[i] != null && carRenderers[i].enabled;
        }
    }

    private void ApplyCarVisibility()
    {
        if (carRenderers == null)
            return;

        bool hide = viewMode == CarCameraViewMode.FirstPerson &&
                    hideCarInFirstPerson;

        for (int i = 0; i < carRenderers.Length; i++)
        {
            if (carRenderers[i] == null)
                continue;

            carRenderers[i].enabled = hide
                ? false
                : carRendererStates != null &&
                  i < carRendererStates.Length &&
                  carRendererStates[i];
        }
    }

    private void OnDestroy()
    {
        // If the camera is removed while the car is still visible in another
        // view, put the body back the way it was.
        if (carRenderers == null)
            return;

        for (int i = 0; i < carRenderers.Length; i++)
        {
            if (carRenderers[i] != null &&
                carRendererStates != null &&
                i < carRendererStates.Length)
            {
                carRenderers[i].enabled = carRendererStates[i];
            }
        }
    }

    private Vector3 ResolveCollision(Vector3 focus, Vector3 desired)
    {
        Vector3 direction = desired - focus;
        float length = direction.magnitude;

        if (length <= 0.01f)
            return desired;

        RaycastHit[] hits = Physics.SphereCastAll(
            focus,
            collisionRadius,
            direction.normalized,
            length,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        float closest = length;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null)
                continue;

            // The car must not push its own camera away.
            if (hitCollider.transform == target ||
                hitCollider.transform.IsChildOf(target))
            {
                continue;
            }

            if (hits[i].distance < closest)
                closest = hits[i].distance;
        }

        if (closest >= length)
            return desired;

        return focus + direction.normalized * Mathf.Max(
            minimumDistance,
            closest - collisionRadius
        );
    }
}
