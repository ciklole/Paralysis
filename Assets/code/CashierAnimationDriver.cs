using UnityEngine;

/// <summary>
/// Procedural idle and dialogue acting for the ordinary cashier. The NPC
/// prefab ships without animation clips, so this driver builds a subtle
/// breathing loop and line-by-line gestures directly from the humanoid rig.
/// Every gesture has a zero-weight start and end, so poses never snap and a
/// new line cannot overlap an unfinished one.
/// </summary>
public class CashierAnimationDriver : MonoBehaviour
{
    private enum Gesture
    {
        None,
        Nod,
        Tilt,
        ExplainRight,
        ExplainLeft,
        Shrug,
        LeanIn,
        LookAside,
        Listen
    }

    [Header("Rig")]
    [SerializeField] private Animator animator;

    [Header("Idle")]
    [SerializeField, Min(0f)] private float idleSpeed = 1f;
    [SerializeField, Min(0f)] private float idleAmount = 1f;

    [Tooltip("Leave negative to detect a T-pose automatically.")]
    [SerializeField] private float armRestAngle = -1f;

    [Header("Gestures")]
    [SerializeField, Min(0.1f)] private float gestureDuration = 1.8f;
    [SerializeField, Min(0.1f)] private float listenGestureDuration = 1.3f;
    [SerializeField, Min(0f)] private float gestureAmount = 1f;

    [Header("Unease")]
    [SerializeField, Min(0.1f)] private float uneaseDuration = 1.6f;

    private HumanoidRig rig;
    private bool initialized;
    private float resolvedArmRestAngle;

    private bool creepy;
    private float creepyBlend;

    private Gesture gesture;
    private float gestureTime;
    private float activeGestureDuration;
    private bool gestureActive;

    private float uneaseTime;
    private bool uneaseActive;

    private float transformationTime;
    private bool transformationActive;

    public bool IsInitialized => initialized;
    public HumanoidRig Rig => rig;

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
                "CashierAnimationDriver: no usable humanoid rig was found.",
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

        // How far the rest pose already hangs below horizontal.
        float downAngle = Mathf.Asin(
            Mathf.Clamp(-armDirection.y, -1f, 1f)
        ) * Mathf.Rad2Deg;

        // Bring a T-pose / A-pose arm to a relaxed ~72 degrees down without
        // over-rotating a rig that already hangs naturally.
        return Mathf.Clamp(72f - downAngle, 0f, 72f);
    }

    public void SetCreepy(bool value)
    {
        creepy = value;
    }

    public void PlayLineGesture(int lineIndex, string speaker)
    {
        if (!initialized)
            Initialize();

        if (!rig.IsValid || transformationActive)
            return;

        // Never cut an unfinished gesture: a new line simply waits for the
        // next one. This is what keeps the arms from popping between poses.
        if (gestureActive && gestureTime < activeGestureDuration)
            return;

        bool isCashier = string.IsNullOrEmpty(speaker) ||
            string.Equals(
                speaker,
                "Cashier",
                System.StringComparison.OrdinalIgnoreCase
            );

        if (!isCashier)
        {
            StartGesture(Gesture.Listen, listenGestureDuration);
            return;
        }

        Gesture next;

        switch (Mathf.Abs(lineIndex) % 8)
        {
            case 0:
                next = Gesture.Nod;
                break;
            case 1:
                next = Gesture.ExplainRight;
                break;
            case 2:
                next = Gesture.Tilt;
                break;
            case 3:
                next = Gesture.LeanIn;
                break;
            case 4:
                next = Gesture.ExplainLeft;
                break;
            case 5:
                next = Gesture.Shrug;
                break;
            case 6:
                next = Gesture.LookAside;
                break;
            default:
                next = Gesture.Nod;
                break;
        }

        StartGesture(next, gestureDuration);
    }

    private void StartGesture(Gesture next, float duration)
    {
        gesture = next;
        gestureTime = 0f;
        activeGestureDuration = Mathf.Max(0.1f, duration);
        gestureActive = true;
    }

    /// <summary>Short, high-frequency shiver before the reveal.</summary>
    public void PlayUnease()
    {
        if (!initialized)
            Initialize();

        gestureActive = false;
        uneaseActive = true;
        uneaseTime = 0f;
    }

    /// <summary>Last pose of the human cashier as the model swaps out.</summary>
    public void PlayTransformation()
    {
        if (!initialized)
            Initialize();

        gestureActive = false;
        uneaseActive = false;
        transformationActive = true;
        transformationTime = 0f;
    }

    public void ResetPose()
    {
        if (rig != null)
            rig.ResetPose();

        gestureActive = false;
        uneaseActive = false;
        transformationActive = false;
        creepy = false;
        creepyBlend = 0f;
    }

    private void LateUpdate()
    {
        if (!initialized)
            Initialize();

        if (rig == null || !rig.IsValid)
            return;

        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime * idleSpeed;

        creepyBlend = Mathf.MoveTowards(
            creepyBlend,
            creepy ? 1f : 0f,
            dt * 1.5f
        );

        rig.ResetPose();

        ApplyIdle(time, dt);
        ApplyGesture(dt);
        ApplyUnease(dt);
        ApplyTransformation(dt);
    }

    private void ApplyIdle(float time, float dt)
    {
        Transform root = rig.Root;
        float amount = idleAmount;

        float breathe = Mathf.Sin(time * 1.55f);
        float sway = Mathf.Sin(time * 0.53f + 0.7f);
        float headNoise = Mathf.PerlinNoise(time * 0.31f, 1.7f) * 2f - 1f;

        rig.RotateWorld(
            HumanBodyBones.Hips,
            root.forward,
            sway * 1.1f * amount
        );

        rig.OffsetHips(new Vector3(
            0f,
            (breathe * 0.006f - 0.003f) * amount,
            0f
        ));

        rig.RotateWorld(
            HumanBodyBones.Spine,
            root.right,
            breathe * 1.2f * amount
        );

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.right,
            breathe * 1.7f * amount
        );

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.up,
            sway * 0.9f * amount
        );

        rig.RotateWorld(
            HumanBodyBones.Neck,
            root.right,
            -breathe * 0.8f * amount
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.up,
            (sway * 1.8f + headNoise * 1.2f) * amount
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.forward,
            (headNoise * 1.5f + sway * 0.8f) * amount
        );

        if (creepyBlend > 0.001f)
        {
            float twitch = Mathf.Sin(time * 9.3f) *
                Mathf.PerlinNoise(time * 2.7f, 4.1f);

            rig.RotateWorld(
                HumanBodyBones.Head,
                root.forward,
                7f * creepyBlend + twitch * 2.5f * creepyBlend
            );

            rig.RotateWorld(
                HumanBodyBones.Head,
                root.right,
                -4f * creepyBlend + twitch * 1.5f * creepyBlend
            );

            rig.RotateWorld(
                HumanBodyBones.Chest,
                root.right,
                -2f * creepyBlend
            );
        }

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
            (sway * 1.2f + breathe * 0.5f) * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.RightUpperArm,
            root.right,
            (-sway * 1.2f + breathe * 0.5f) * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.LeftLowerArm,
            root.right,
            4f * idleAmount
        );

        rig.RotateWorld(
            HumanBodyBones.RightLowerArm,
            root.right,
            4f * idleAmount
        );
    }

    private void ApplyGesture(float dt)
    {
        if (!gestureActive)
            return;

        gestureTime += dt;

        float t = Mathf.Clamp01(gestureTime / activeGestureDuration);
        float envelope = Mathf.Sin(Mathf.PI * t);

        if (t >= 1f)
            gestureActive = false;

        if (envelope <= 0.0001f)
            return;

        Transform root = rig.Root;
        float amount = envelope * gestureAmount;

        switch (gesture)
        {
            case Gesture.Nod:
                rig.RotateWorld(
                    HumanBodyBones.Head,
                    root.right,
                    10f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.Chest,
                    root.right,
                    2.5f * amount
                );
                break;

            case Gesture.Tilt:
                rig.RotateWorld(
                    HumanBodyBones.Head,
                    root.forward,
                    12f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.Head,
                    root.up,
                    5f * amount
                );
                break;

            case Gesture.LeanIn:
                rig.RotateWorld(
                    HumanBodyBones.Spine,
                    root.right,
                    5f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.Chest,
                    root.right,
                    4f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.Head,
                    root.right,
                    -2f * amount
                );
                break;

            case Gesture.LookAside:
                rig.RotateWorld(
                    HumanBodyBones.Head,
                    root.up,
                    16f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.Chest,
                    root.up,
                    3f * amount
                );
                break;

            case Gesture.Shrug:
                rig.RotateWorld(
                    HumanBodyBones.LeftShoulder,
                    root.forward,
                    10f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.RightShoulder,
                    root.forward,
                    -10f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.LeftUpperArm,
                    root.forward,
                    12f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.RightUpperArm,
                    root.forward,
                    -12f * amount
                );
                break;

            case Gesture.ExplainRight:
                Explain(root, true, amount);
                break;

            case Gesture.ExplainLeft:
                Explain(root, false, amount);
                break;

            case Gesture.Listen:
                rig.RotateWorld(
                    HumanBodyBones.Head,
                    root.forward,
                    7f * amount
                );
                rig.RotateWorld(
                    HumanBodyBones.Head,
                    root.right,
                    4f * amount
                );
                break;
        }
    }

    private void Explain(Transform root, bool rightHand, float amount)
    {
        HumanBodyBones upper = rightHand
            ? HumanBodyBones.RightUpperArm
            : HumanBodyBones.LeftUpperArm;

        HumanBodyBones lower = rightHand
            ? HumanBodyBones.RightLowerArm
            : HumanBodyBones.LeftLowerArm;

        HumanBodyBones hand = rightHand
            ? HumanBodyBones.RightHand
            : HumanBodyBones.LeftHand;

        Vector3 chest = rig.GetWorldPosition(HumanBodyBones.Chest);
        float side = rightHand ? 1f : -1f;

        Vector3 target = chest +
            root.forward * 0.45f +
            root.right * (0.12f * side) +
            root.up * 0.08f;

        Vector3 pole = chest +
            root.right * (0.65f * side) +
            root.up * 0.2f;

        rig.SolveTwoBoneIK(
            upper,
            lower,
            hand,
            target,
            pole,
            amount,
            Vector3.zero
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.right,
            3f * amount
        );
    }

    private void ApplyUnease(float dt)
    {
        if (!uneaseActive)
            return;

        uneaseTime += dt;

        float t = Mathf.Clamp01(uneaseTime / uneaseDuration);
        float envelope = Mathf.Sin(Mathf.PI * t);

        if (t >= 1f)
            uneaseActive = false;

        if (envelope <= 0.0001f)
            return;

        Transform root = rig.Root;
        float shake = Mathf.Sin(uneaseTime * 42f) * envelope;
        float noise = Mathf.PerlinNoise(uneaseTime * 18f, 2.3f) * 2f - 1f;

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.right,
            (noise * 2.5f + shake * 1.5f) * envelope
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.forward,
            (shake * 5f + noise * 3f) * envelope
        );

        rig.RotateWorld(
            HumanBodyBones.LeftUpperArm,
            root.right,
            shake * 2f * envelope
        );

        rig.RotateWorld(
            HumanBodyBones.RightUpperArm,
            root.right,
            -shake * 2f * envelope
        );
    }

    private void ApplyTransformation(float dt)
    {
        if (!transformationActive)
            return;

        transformationTime += dt;

        float t = Mathf.Clamp01(transformationTime / 0.8f);
        float envelope = Mathf.Sin(Mathf.PI * t);

        if (t >= 1f)
            transformationActive = false;

        if (envelope <= 0.0001f)
            return;

        Transform root = rig.Root;
        float shake = Mathf.Sin(transformationTime * 55f) * envelope;

        rig.RotateWorld(
            HumanBodyBones.Spine,
            root.right,
            -8f * envelope
        );

        rig.RotateWorld(
            HumanBodyBones.Chest,
            root.right,
            -6f * envelope + shake * 2f
        );

        rig.RotateWorld(
            HumanBodyBones.Head,
            root.right,
            10f * envelope + shake * 4f
        );

        rig.RotateWorld(
            HumanBodyBones.LeftUpperArm,
            root.forward,
            18f * envelope
        );

        rig.RotateWorld(
            HumanBodyBones.RightUpperArm,
            root.forward,
            -18f * envelope
        );
    }
}
