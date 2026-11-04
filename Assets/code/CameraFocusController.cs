using System.Collections;
using UnityEngine;

/// <summary>
/// Moves the first-person camera to authored anchors and returns it exactly
/// where the player controller left it. This is the shared camera-lock path
/// used by the cashier sequence; it follows the same rules as
/// <see cref="InteractableObject"/>: snapshot the camera's local transform,
/// lock PlayerControl, animate in world space with an unscaled smooth step,
/// then restore the local transform and near plane.
/// </summary>
public class CameraFocusController : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Near plane used while the camera is close to a character.")]
    [SerializeField, Min(0.01f)] private float focusNearClipPlane = 0.05f;

    [SerializeField, Min(0.01f)] private float defaultTransitionDuration = 1.2f;

    [Header("Collision")]
    [Tooltip(
        "Pull the camera in when a wall is between the look target and the " +
        "anchor. Authored cashier anchors are clear, so this stays optional.")]
    [SerializeField] private bool avoidCameraClipping;

    [SerializeField] private LayerMask cameraCollisionMask = ~0;
    [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.15f;
    [SerializeField, Min(0.01f)] private float minimumCameraDistance = 0.25f;

    [Tooltip("Models that must never push the camera around.")]
    [SerializeField] private Transform[] collisionIgnores;

    private FirstPersonController controller;
    private Camera cam;
    private Coroutine transition;

    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private float savedNearClipPlane;
    private bool hasSavedState;
    private bool active;

    public bool IsActive => active;
    public bool IsTransitioning => transition != null;
    public Camera Camera => cam;
    public FirstPersonController Controller => controller;

    public bool Focus(
        Transform positionAnchor,
        Transform lookTarget,
        float duration = -1f)
    {
        if (positionAnchor == null || lookTarget == null)
            return false;

        if (!EnsureReady())
            return false;

        if (!active)
        {
            active = true;

            savedLocalPosition = cam.transform.localPosition;
            savedLocalRotation = cam.transform.localRotation;
            savedNearClipPlane = cam.nearClipPlane;
            hasSavedState = true;

            cam.nearClipPlane = Mathf.Min(
                cam.nearClipPlane,
                focusNearClipPlane
            );

            PlayerControl.Lock(this, controller);
            UIState.PushModal(this);
        }

        MoveTo(positionAnchor, lookTarget, duration);
        return true;
    }

    public void MoveTo(
        Transform positionAnchor,
        Transform lookTarget,
        float duration = -1f)
    {
        if (!active || positionAnchor == null || lookTarget == null)
            return;

        Vector3 position = ResolvePosition(
            positionAnchor.position,
            lookTarget.position
        );

        Quaternion rotation = Quaternion.LookRotation(
            lookTarget.position - position,
            Vector3.up
        );

        StartTransition(
            position,
            rotation,
            duration < 0f ? defaultTransitionDuration : duration,
            false
        );
    }

    public void MoveTo(
        Vector3 worldPosition,
        Vector3 worldLookAt,
        float duration = -1f)
    {
        if (!active)
            return;

        Vector3 position = ResolvePosition(worldPosition, worldLookAt);

        Quaternion rotation = Quaternion.LookRotation(
            worldLookAt - position,
            Vector3.up
        );

        StartTransition(
            position,
            rotation,
            duration < 0f ? defaultTransitionDuration : duration,
            false
        );
    }

    public void ReturnHome(float duration = -1f)
    {
        if (!active || cam == null)
        {
            Abort();
            return;
        }

        Vector3 homePosition = HomePosition();
        Quaternion homeRotation = HomeRotation();

        StartTransition(
            homePosition,
            homeRotation,
            duration < 0f ? defaultTransitionDuration : duration,
            true
        );
    }

    /// <summary>
    /// Restores the camera immediately. Safe to call repeatedly and from
    /// OnDisable/OnDestroy, so a disabled cashier can never leave the camera
    /// detached from the player.
    /// </summary>
    public void Abort()
    {
        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }

        RestoreCamera();
        active = false;

        UIState.PopModal(this);
        PlayerControl.Unlock(this);
    }

    private bool EnsureReady()
    {
        if (active && cam != null && controller != null)
            return true;

        controller = InteractionUtil.FindController();

        if (controller == null)
        {
            Debug.LogWarning(
                "CameraFocusController: no player controller found.",
                this
            );
            return false;
        }

        cam = controller.playerCamera != null
            ? controller.playerCamera
            : InteractionUtil.FindCamera();

        if (cam == null)
        {
            Debug.LogWarning(
                "CameraFocusController: no player camera found.",
                this
            );
            return false;
        }

        return true;
    }

    private Vector3 HomePosition()
    {
        Transform parent = cam.transform.parent;

        return parent != null
            ? parent.TransformPoint(savedLocalPosition)
            : savedLocalPosition;
    }

    private Quaternion HomeRotation()
    {
        Transform parent = cam.transform.parent;

        return parent != null
            ? parent.rotation * savedLocalRotation
            : savedLocalRotation;
    }

    private void RestoreCamera()
    {
        if (cam == null)
            return;

        cam.transform.localPosition = savedLocalPosition;
        cam.transform.localRotation = savedLocalRotation;

        if (hasSavedState)
        {
            cam.nearClipPlane = savedNearClipPlane;
            hasSavedState = false;
        }
    }

    private void StartTransition(
        Vector3 worldPosition,
        Quaternion worldRotation,
        float duration,
        bool restoreOnFinish)
    {
        if (transition != null)
            StopCoroutine(transition);

        transition = StartCoroutine(
            MoveCamera(worldPosition, worldRotation, duration, restoreOnFinish)
        );
    }

    private IEnumerator MoveCamera(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        bool restoreOnFinish)
    {
        if (cam == null)
        {
            transition = null;
            Abort();
            yield break;
        }

        Vector3 startPosition = cam.transform.position;
        Quaternion startRotation = cam.transform.rotation;

        duration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            cam.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            cam.transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        cam.transform.position = targetPosition;
        cam.transform.rotation = targetRotation;
        transition = null;

        if (!restoreOnFinish)
            yield break;

        RestoreCamera();
        active = false;

        UIState.PopModal(this);
        PlayerControl.Unlock(this);
    }

    private Vector3 ResolvePosition(Vector3 desired, Vector3 lookAt)
    {
        if (!avoidCameraClipping)
            return desired;

        Vector3 direction = desired - lookAt;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return desired;

        direction /= distance;

        RaycastHit[] hits = Physics.SphereCastAll(
            lookAt,
            cameraCollisionRadius,
            direction,
            distance,
            cameraCollisionMask,
            QueryTriggerInteraction.Ignore
        );

        float closest = distance;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i].collider;

            if (hit == null || ShouldIgnoreCollision(hit.transform))
                continue;

            if (hits[i].distance < closest)
                closest = hits[i].distance;
        }

        if (closest >= distance)
            return desired;

        float safeDistance = Mathf.Max(
            minimumCameraDistance,
            closest - cameraCollisionRadius
        );

        return lookAt + direction * safeDistance;
    }

    private bool ShouldIgnoreCollision(Transform hit)
    {
        if (controller != null && hit.IsChildOf(controller.transform))
            return true;

        if (collisionIgnores == null)
            return false;

        for (int i = 0; i < collisionIgnores.Length; i++)
        {
            Transform ignore = collisionIgnores[i];

            if (ignore != null && (hit == ignore || hit.IsChildOf(ignore)))
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        if (active)
            Abort();
    }

    private void OnDestroy()
    {
        if (active)
            Abort();
    }
}
