using UnityEngine;

/// <summary>
/// Procedural animation for the pumpkin monster. It supplies a heavy idle, a
/// distance-driven walk cycle so the feet do not slide, and a two-handed
/// reach with anticipation and follow-through. The reach uses the same
/// humanoid IK helper as the cashier, so both hands actually point at the
/// camera instead of playing a canned pose.
/// </summary>
public class MonsterAnimationDriver : MonoBehaviour
{
    [Header("Rig")]
    [SerializeField] private Animator animator;

    [Header("Idle")]
    [SerializeField, Min(0f)] private float idleSpeed = 1f;
    [SerializeField, Min(0f)] private float idleAmount = 1f;

    [Tooltip("Leave negative to detect a T-pose automatically.")]
    [SerializeField] private float armRestAngle = -1f;

    [Header("Walk")]
    [SerializeField, Min(0f)] private float walkCycleSpeed = 1.35f;
    [SerializeField, Min(0f)] private float strideAngle = 26f;
    [SerializeField, Min(0f)] private float armSwingAngle = 18f;
    [SerializeField, Min(0.1f)] private float locomotionBlendSpeed = 6f;

    [Header("Reach")]
    [SerializeField, Min(0.5f)] private float defaultReachDuration = 2.4f;
    [SerializeField, Min(0f)] private float handSpread = 0.24f;
    [SerializeField] private Vector3 handAimEuler = Vector3.zero;

    private HumanoidRig rig;
    private bool initialized;
    private float resolvedArmRestAngle;

    private float time;
    private float walkPhase;
    private float locomotion;
    private float locomotionTarget;

    private bool reachActive;
    private bool reachReleasing;
    private float reachReleaseTime;
    private float reachReleaseDuration;
    private float reachTime;
    private float reachDuration;
    private Vector3 reachTarget;
    private Transform reachTargetTransform;
    private bool hasReachTransform;

    public bool IsInitialized => initialized;
    public bool IsReaching => reachActive;
    public float ReachProgress => reachDuration > 0f
        ? Mathf.Clamp01(reachTime / reachDuration)
        : 0f;

    public bool ReachComplete => reachActive && reachTime >= reachDuration;

    /// <summary>World-space chest/head midpoint used by the camera.</summary>
    public Vector3 FocusPoint
    {
        get
        {
            if (rig == null || !rig.IsValid)
                return transform.position + Vector3.up * 1.6f;

            Vector3 chest = rig.GetWorldPosition(HumanBodyBones.Chest);
            Vector3 head = rig.GetWorldPosition(HumanBodyBones.Head);

            return Vector3.Lerp(chest, head, 0.55f);
        }
    }

    public Vector3 GetBonePosition(HumanBodyBones bone)
    {
        return rig != null
            ? rig.GetWorldPosition(bone)
            : transform.position;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        if (rig != null)
            rig.ResetPose();
    }

    public void Initialize()
    {
        if (initialized)
            return;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        rig = new HumanoidRig(animator);
        initialized = true;

        resolvedArmRestAngle = armRestAngle >= 0f
            ? armRestAngle
            : DetectArmRestAngle();

        if (!rig.IsValid)
        {
            Debug.LogWarning(
                "MonsterAnimationDriver: no usable humanoid rig was found.",
                this
            );
        }
    }

    private float DetectArmRestAngle()
    {
        if (rig == null || !rig.IsValid)
            return 0f;

        Transform upper = rig.Get(HumanBodyBones.LeftUpperArm);
        Transform lower = rig.Get(HumanBodyBones.LeftLowerArm);
        Transform root = rig.Root;

        if (upper == null || lower == null || root == null)
            return 0f;

        Vector3 armDirection = (lower.position - upper.position).normalized;
        float horizontal = Mathf.Abs(Vector3.Dot(armDirection, root.right));

        if (horizontal < 0.4f)
            return 0f;

        // How far the rest pose already hangs below horizontal. The pumpkin's
        // rest pose is an A-pose, so this only adds the remaining rotation
        // needed to reach a relaxed ~72 degrees.
        float downAngle = Mathf.Asin(
            Mathf.Clamp(-armDirection.y, -1f, 1f)
        ) * Mathf.Rad2Deg;

        return Mathf.Clamp(72f - downAngle, 0f, 72f);
    }

    /// <summary>Resets the pose and prepares the rig for its first frame.</summary>
    public void Prepare()
    {
        Initialize();

        if (rig != null)
            rig.ResetPose();

        time = 0f;
        walkPhase = 0f;
        locomotion = 0f;
        locomotionTarget = 0f;
        reachActive = false;
        reachReleasing = false;
        reachTime = 0f;
    }

    /// <summary>0 = standing, 1 = full walk speed.</summary>
    public void SetLocomotion(float normalizedSpeed)
    {
        locomotionTarget = Mathf.Clamp01(normalizedSpeed);
    }

    public void BeginReach(Transform target, float duration = -1f)
    {
        if (!initialized)
            Initialize();

        if (target == null)
            return;

        reachTargetTransform = target;
        hasReachTransform = true;
        BeginReachInternal(duration);
    }

    public void BeginReach(Vector3 target, float duration = -1f)
    {
        if (!initialized)
            Initialize();

        reachTargetTransform = null;
        hasReachTransform = false;
        reachTarget = target;
        BeginReachInternal(duration);
    }

    private void BeginReachInternal(float duration)
    {
        reachActive = true;
        reachReleasing = false;
        reachReleaseTime = 0f;
        reachTime = 0f;
        reachDuration = duration > 0f
            ? duration
            : Mathf.Max(0.5f, defaultReachDuration);
    }

    /// <summary>
    /// Freezes the current hand target and blends the arms back to the idle
    /// pose. Used when the camera starts pulling away so the hands do not
    /// chase the lens across the room.
    /// </summary>
    public void ReleaseReach(float duration = 0.6f)
    {
        if (!reachActive || reachReleasing)
            return;

        reachTarget = ResolveReachTarget();
        hasReachTransform = false;
        reachReleasing = true;
        reachReleaseTime = 0f;
        reachReleaseDuration = Mathf.Max(0.0001f, duration);
    }

    public void CancelReach()
    {
        reachActive = false;
        reachReleasing = false;
        reachTime = 0f;
    }

    public void ResetPose()
    {
        if (rig != null)
            rig.ResetPose();

        reachActive = false;
        reachReleasing = false;
        locomotion = 0f;
        locomotionTarget = 0f;
    }

    private void LateUpdate()
    {
        if (!initialized)
            Initialize();

        if (rig == null || !rig.IsValid)
            return;

        float dt = Time.unscaledDeltaTime;
        time += dt;

        locomotion = Mathf.MoveTowards(
            locomotion,
            locomotionTarget,
            locomotionBlendSpeed * dt
        );

        rig.ResetPose();

        ApplyIdle();
        ApplyWalk(dt);
        ApplyReach(dt);
    }

    private void ApplyIdle()
    {
        Transform root = rig.Root;
        float breathe = Mathf.Sin(time * idleSpeed * 1.25f);
        float sway = Mathf.Sin(time * idleSpeed * 0.47f + 1.3f);
        float twitch = Mathf.PerlinNoise(time * 0.37f, 7.1f) * 2f - 1f;

        rig.RotateWorld(
            HumanBodyBones.Hips,
            root.forward,
            sway * 1.6f * idleAmount
        );

        rig.OffsetHips(new Vector3(
            0f,
            breathe * 0.012f * idleAmount,
            0f
        ));

        rig.RotateWorld(
            HumanBodyBones.Spine,
            root.right,
            breathe * 2.2f * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.right,
            breathe * 2.8f * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.up,
            sway * 1.5f * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.Neck,
            root.right,
            -breathe * 1.2f * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.forward,
            (twitch * 4f + sway * 1.5f) * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.up,
            (twitch * 5f + sway * 2f) * idleAmount
        );

        ApplyArmRest(root, sway, breathe);
    }

    private void ApplyArmRest(Transform root, float sway, float breathe)
    {
        float angle = resolvedArmRestAngle;

        if (angle > 0.001f)
        {
            rig.RotateWorld(
                HumanBodyBones.LeftUpperArm,
                root.forward,
                angle
            );

            rig.RotateWorld(
                HumanBodyBones.RightUpperArm,
                root.forward,
                -angle
            );
        }

        rig.RotateWorld(
            HumanBodyBones.LeftUpperArm,
            root.right,
            (sway * 2.5f + breathe * 1.2f) * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.RightUpperArm,
            root.right,
            (-sway * 2.5f + breathe * 1.2f) * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.LeftLowerArm,
            root.right,
            7f * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.RightLowerArm,
            root.right,
            7f * idleAmount
        );
    }

    private void ApplyWalk(float dt)
    {
        if (locomotion <= 0.001f)
            return;

        Transform root = rig.Root;
        walkPhase += dt * walkCycleSpeed * (0.35f + locomotion * 1.15f);

        float swing = Mathf.Sin(walkPhase);
        float counter = Mathf.Sin(walkPhase + Mathf.PI);
        float bob = Mathf.Abs(Mathf.Sin(walkPhase));

        float stride = strideAngle * locomotion;
        float armSwing = armSwingAngle * locomotion;

        rig.OffsetHips(new Vector3(
            0f,
            (bob - 0.5f) * 0.045f * locomotion,
            0f
        ));

        rig.RotateWorld(
            HumanBodyBones.Hips,
            root.up,
            swing * 3f * locomotion
        );

        rig.RotateWorld(
            HumanBodyBones.LeftUpperLeg,
            root.right,
            swing * stride
        );

        rig.RotateWorld(
            HumanBodyBones.RightUpperLeg,
            root.right,
            counter * stride
        );

        rig.RotateWorld(
            HumanBodyBones.LeftLowerLeg,
            root.right,
            Mathf.Max(0f, -swing) * stride * 1.3f
        );

        rig.RotateWorld(
            HumanBodyBones.RightLowerLeg,
            root.right,
            Mathf.Max(0f, -counter) * stride * 1.3f
        );

        rig.RotateWorld(
            HumanBodyBones.LeftFoot,
            root.right,
            -swing * stride * 0.35f
        );

        rig.RotateWorld(
            HumanBodyBones.RightFoot,
            root.right,
            -counter * stride * 0.35f
        );

        rig.RotateWorld(
            HumanBodyBones.LeftUpperArm,
            root.right,
            counter * armSwing
        );

        rig.RotateWorld(
            HumanBodyBones.RightUpperArm,
            root.right,
            swing * armSwing
        );

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.up,
            -swing * 2.5f * locomotion
        );
    }

    private void ApplyReach(float dt)
    {
        if (!reachActive)
            return;

        if (reachReleasing)
        {
            reachReleaseTime += dt;

            if (reachReleaseTime >= reachReleaseDuration)
            {
                reachActive = false;
                reachReleasing = false;
                reachTime = 0f;
                return;
            }
        }
        else
        {
            reachTime += dt;
        }

        float t = Mathf.Clamp01(reachTime / reachDuration);
        float anticipation = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.22f));
        float extend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.16f) / 0.3f));
        float settle = 1f - 0.06f * Mathf.Sin(
            Mathf.PI * Mathf.Clamp01((t - 0.46f) / 0.3f)
        );

        float amount = extend * settle;

        if (reachReleasing)
        {
            amount *= 1f - Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(reachReleaseTime / reachReleaseDuration)
            );
        }

        Transform root = rig.Root;

        rig.RotateWorld(
            HumanBodyBones.Spine,
            root.right,
            -5f * anticipation + 7f * amount
        );

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.right,
            -4f * anticipation + 6f * amount
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.right,
            3f * anticipation - 4f * amount
        );

        Vector3 target = ResolveReachTarget();
        Vector3 chest = rig.GetWorldPosition(HumanBodyBones.Chest);

        Vector3 rightPole = chest +
            root.right * 0.9f +
            root.up * 0.25f;

        Vector3 leftPole = chest -
            root.right * 0.9f +
            root.up * 0.25f;

        Vector3 rightTarget = target + root.right * handSpread;
        Vector3 leftTarget = target - root.right * handSpread;

        rig.SolveTwoBoneIK(
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
            rightTarget,
            rightPole,
            amount,
            handAimEuler
        );

        rig.SolveTwoBoneIK(
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            leftTarget,
            leftPole,
            amount,
            handAimEuler
        );

        AimHeadAt(target, amount * 0.75f);
    }

    private Vector3 ResolveReachTarget()
    {
        if (!hasReachTransform || reachTargetTransform == null)
            return reachTarget;

        return reachTargetTransform.position +
            reachTargetTransform.forward * 0.15f -
            reachTargetTransform.up * 0.05f;
    }

    private void AimHeadAt(Vector3 target, float weight)
    {
        if (weight <= 0.0001f)
            return;

        Transform head = rig.Get(HumanBodyBones.Head);
        Transform root = rig.Root;

        if (head == null || root == null)
            return;

        Vector3 direction = target - head.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        Vector3 flat = Vector3.ProjectOnPlane(direction, root.up);

        if (flat.sqrMagnitude < 0.0001f)
            return;

        flat.Normalize();

        float yaw = Vector3.SignedAngle(root.forward, flat, root.up);
        float pitch = Vector3.SignedAngle(flat, direction, root.right);

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.up,
            yaw * weight
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.right,
            -pitch * weight
        );
    }
}
