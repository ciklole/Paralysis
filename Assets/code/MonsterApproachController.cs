using System.Collections;
using UnityEngine;

/// <summary>
/// Moves the pumpkin from an off-screen start anchor along authored waypoints
/// to the checkout counter. Speed accelerates and decelerates smoothly, the
/// model turns to face its travel direction and then the camera, and the walk
/// cycle is driven by actual distance travelled so the feet do not slide.
/// </summary>
public class MonsterApproachController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform monsterRoot;
    [SerializeField] private MonsterAnimationDriver animationDriver;

    [Tooltip("Start anchor followed by every waypoint, ending in front of the counter.")]
    [SerializeField] private Transform[] pathPoints;

    [Tooltip("Usually the player camera; the monster faces this at the end.")]
    [SerializeField] private Transform faceTarget;

    [Header("Movement")]
    [SerializeField, Min(0.05f)] private float moveSpeed = 1.15f;
    [SerializeField, Min(0.05f)] private float acceleration = 1.4f;
    [SerializeField, Min(1f)] private float turnSpeed = 150f;
    [SerializeField, Min(0.01f)] private float waypointTolerance = 0.06f;
    [SerializeField, Min(0.1f)] private float finalSlowdownDistance = 1.6f;

    [Header("Model Orientation")]
    [Tooltip(
        "Extra yaw applied after LookRotation. 0 when the model's visual " +
        "forward is its transform forward.")]
    [SerializeField] private float modelYawOffset;

    [Header("Ground")]
    [SerializeField] private bool snapToGround;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayHeight = 2f;
    [SerializeField] private float groundOffset;

    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private bool hasSavedTransform;

    private float currentSpeed;
    private bool approaching;
    private bool cancelled;

    public float CurrentSpeed => currentSpeed;
    public float NormalizedSpeed => moveSpeed > 0f
        ? Mathf.Clamp01(currentSpeed / moveSpeed)
        : 0f;

    public bool IsApproaching => approaching;
    public Transform MonsterRoot => monsterRoot;
    public Transform FaceTarget => faceTarget;

    /// <summary>Lets the sequence retarget the monster at the live camera.</summary>
    public void SetFaceTarget(Transform target)
    {
        faceTarget = target;
    }

    /// <summary>World-space chest/head point for the reach camera.</summary>
    public Vector3 FocusPoint
    {
        get
        {
            if (animationDriver != null && animationDriver.IsInitialized)
                return animationDriver.FocusPoint;

            return monsterRoot != null
                ? monsterRoot.position + Vector3.up * 1.6f
                : transform.position;
        }
    }

    private void Awake()
    {
        if (animationDriver == null && monsterRoot != null)
            animationDriver = monsterRoot.GetComponentInChildren<MonsterAnimationDriver>(true);

        // The live player camera is the final look target. The scene builder
        // may also assign an anchor explicitly.
        if (faceTarget == null)
        {
            Camera cam = InteractionUtil.FindCamera();

            if (cam != null)
                faceTarget = cam.transform;
        }
    }

    public void Prepare()
    {
        if (monsterRoot == null)
            return;

        cancelled = false;
        SaveTransform();

        if (pathPoints != null && pathPoints.Length > 0 && pathPoints[0] != null)
            monsterRoot.position = pathPoints[0].position;

        currentSpeed = 0f;
        approaching = false;

        FaceImmediately(faceTarget != null
            ? faceTarget.position - monsterRoot.position
            : monsterRoot.forward);

        if (animationDriver != null)
            animationDriver.Prepare();

        SnapToGround();
    }

    public IEnumerator ApproachRoutine()
    {
        if (monsterRoot == null || pathPoints == null || pathPoints.Length < 2)
            yield break;

        Prepare();
        approaching = true;

        for (int pointIndex = 1; pointIndex < pathPoints.Length; pointIndex++)
        {
            Transform point = pathPoints[pointIndex];

            if (point == null)
                continue;

            bool lastPoint = pointIndex == pathPoints.Length - 1;

            while (true)
            {
                if (cancelled)
                {
                    approaching = false;
                    yield break;
                }

                Vector3 target = point.position;
                Vector3 flatToTarget = target - monsterRoot.position;
                flatToTarget.y = 0f;

                float distance = flatToTarget.magnitude;

                if (distance <= waypointTolerance)
                    break;

                float desiredSpeed = moveSpeed;

                if (lastPoint && distance < finalSlowdownDistance)
                {
                    desiredSpeed *= Mathf.Clamp01(
                        distance / finalSlowdownDistance
                    );
                }

                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    desiredSpeed,
                    acceleration * Time.unscaledDeltaTime
                );

                float step = Mathf.Min(
                    currentSpeed * Time.unscaledDeltaTime,
                    distance
                );

                monsterRoot.position = Vector3.MoveTowards(
                    monsterRoot.position,
                    target,
                    step
                );

                Vector3 faceDirection = lastPoint && distance < 0.75f
                    ? ResolveFaceDirection()
                    : flatToTarget;

                Face(faceDirection, Time.unscaledDeltaTime);

                if (animationDriver != null)
                {
                    animationDriver.SetLocomotion(
                        Mathf.Clamp01(currentSpeed / moveSpeed)
                    );
                }

                SnapToGround();
                yield return null;
            }
        }

        currentSpeed = 0f;

        if (animationDriver != null)
            animationDriver.SetLocomotion(0f);

        // Settle the final facing direction before the reach begins.
        float settle = 0f;

        while (settle < 0.35f)
        {
            if (cancelled)
            {
                approaching = false;
                yield break;
            }

            settle += Time.unscaledDeltaTime;

            Face(ResolveFaceDirection(), Time.unscaledDeltaTime);
            SnapToGround();
            yield return null;
        }

        approaching = false;
    }

    private Vector3 ResolveFaceDirection()
    {
        if (faceTarget == null)
            return monsterRoot.forward;

        Vector3 direction = faceTarget.position - monsterRoot.position;
        direction.y = 0f;

        return direction.sqrMagnitude > 0.0001f
            ? direction
            : monsterRoot.forward;
    }

    private void Face(Vector3 direction, float dt)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up) *
            Quaternion.Euler(0f, modelYawOffset, 0f);

        monsterRoot.rotation = Quaternion.RotateTowards(
            monsterRoot.rotation,
            targetRotation,
            turnSpeed * dt
        );
    }

    private void FaceImmediately(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        monsterRoot.rotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up) *
            Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    private void SnapToGround()
    {
        if (!snapToGround || monsterRoot == null)
            return;

        Vector3 origin = monsterRoot.position + Vector3.up * groundRayHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            groundRayHeight * 2f,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        float best = float.MaxValue;
        float height = monsterRoot.position.y;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i].collider;

            if (hit == null ||
                hit.transform == monsterRoot ||
                hit.transform.IsChildOf(monsterRoot))
            {
                continue;
            }

            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                height = hits[i].point.y + groundOffset;
            }
        }

        if (best < float.MaxValue)
        {
            Vector3 position = monsterRoot.position;
            position.y = height;
            monsterRoot.position = position;
        }
    }

    public void RestoreOriginalTransform()
    {
        cancelled = true;

        if (!hasSavedTransform || monsterRoot == null)
            return;

        monsterRoot.localPosition = savedLocalPosition;
        monsterRoot.localRotation = savedLocalRotation;
        hasSavedTransform = false;

        if (animationDriver != null)
            animationDriver.ResetPose();

        currentSpeed = 0f;
        approaching = false;
    }

    private void SaveTransform()
    {
        if (hasSavedTransform || monsterRoot == null)
            return;

        savedLocalPosition = monsterRoot.localPosition;
        savedLocalRotation = monsterRoot.localRotation;
        hasSavedTransform = true;
    }

    private void OnDisable()
    {
        if (hasSavedTransform)
            RestoreOriginalTransform();
    }
}
