using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

/// <summary>
/// Small procedural-pose helper for a humanoid rig. It caches the rest pose,
/// resets it every frame, and applies additive world-space rotations or a
/// two-bone arm IK solve. Both store characters ship without animation clips,
/// so this is the shared animation foundation for them.
/// </summary>
[NoAutoStaticsCleanup]
public class HumanoidRig
{
    private static readonly HumanBodyBones[] PoseOrder =
    {
        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.UpperChest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightFoot
    };

    private static readonly Dictionary<HumanBodyBones, string[]> FallbackNames =
        new Dictionary<HumanBodyBones, string[]>
        {
            { HumanBodyBones.Hips, new[] { "Hips", "Character1_Hips" } },
            { HumanBodyBones.Spine, new[] { "Spine", "Character1_Spine" } },
            { HumanBodyBones.Chest, new[] { "Spine1", "Character1_Spine1" } },
            { HumanBodyBones.UpperChest, new[] { "Spine2", "Character1_Spine2" } },
            { HumanBodyBones.Neck, new[] { "Neck", "Character1_Neck" } },
            { HumanBodyBones.Head, new[] { "Head", "Character1_Head" } },
            { HumanBodyBones.LeftShoulder, new[] { "LeftShoulder", "Character1_LeftShoulder" } },
            { HumanBodyBones.LeftUpperArm, new[] { "LeftArm", "Character1_LeftArm" } },
            { HumanBodyBones.LeftLowerArm, new[] { "LeftForeArm", "Character1_LeftForeArm" } },
            { HumanBodyBones.LeftHand, new[] { "LeftHand", "Character1_LeftHand" } },
            { HumanBodyBones.RightShoulder, new[] { "RightShoulder", "Character1_RightShoulder" } },
            { HumanBodyBones.RightUpperArm, new[] { "RightArm", "Character1_RightArm" } },
            { HumanBodyBones.RightLowerArm, new[] { "RightForeArm", "Character1_RightForeArm" } },
            { HumanBodyBones.RightHand, new[] { "RightHand", "Character1_RightHand" } },
            { HumanBodyBones.LeftUpperLeg, new[] { "LeftUpLeg", "Character1_LeftUpLeg" } },
            { HumanBodyBones.LeftLowerLeg, new[] { "LeftLeg", "Character1_LeftLeg" } },
            { HumanBodyBones.LeftFoot, new[] { "LeftFoot", "Character1_LeftFoot" } },
            { HumanBodyBones.RightUpperLeg, new[] { "RightUpLeg", "Character1_RightUpLeg" } },
            { HumanBodyBones.RightLowerLeg, new[] { "RightLeg", "Character1_RightLeg" } },
            { HumanBodyBones.RightFoot, new[] { "RightFoot", "Character1_RightFoot" } }
        };

    private readonly Animator animator;
    private readonly Transform root;
    private readonly Dictionary<HumanBodyBones, Transform> bones =
        new Dictionary<HumanBodyBones, Transform>();

    private readonly Dictionary<Transform, Quaternion> baseLocalRotations =
        new Dictionary<Transform, Quaternion>();

    private readonly Dictionary<Transform, Vector3> baseLocalPositions =
        new Dictionary<Transform, Vector3>();

    public bool IsValid => root != null && bones.Count >= 6;
    public Transform Root => root;
    public Animator Animator => animator;

    public HumanoidRig(Animator source)
    {
        animator = source;
        root = source != null ? source.transform : null;

        ResolveBones();
        CacheRestPose();
    }

    private void ResolveBones()
    {
        if (animator == null)
            return;

        for (int i = 0; i < PoseOrder.Length; i++)
        {
            HumanBodyBones bone = PoseOrder[i];
            Transform transform = null;

            try
            {
                transform = animator.GetBoneTransform(bone);
            }
            catch (Exception)
            {
                // An animator without a valid avatar cannot map bones.
            }

            if (transform == null)
                transform = FindFallback(bone);

            if (transform != null)
                bones[bone] = transform;
        }
    }

    private Transform FindFallback(HumanBodyBones bone)
    {
        if (root == null)
            return null;

        if (!FallbackNames.TryGetValue(bone, out string[] names))
            return null;

        for (int i = 0; i < names.Length; i++)
        {
            Transform found = FindDeepChild(root, names[i]);

            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), name);

            if (found != null)
                return found;
        }

        return null;
    }

    private void CacheRestPose()
    {
        foreach (KeyValuePair<HumanBodyBones, Transform> pair in bones)
        {
            Transform bone = pair.Value;

            if (bone == null)
                continue;

            baseLocalRotations[bone] = bone.localRotation;
            baseLocalPositions[bone] = bone.localPosition;
        }
    }

    public Transform Get(HumanBodyBones bone)
    {
        bones.TryGetValue(bone, out Transform transform);
        return transform;
    }

    public Vector3 GetWorldPosition(HumanBodyBones bone)
    {
        Transform transform = Get(bone);
        return transform != null ? transform.position : root.position;
    }

    public Quaternion GetWorldRotation(HumanBodyBones bone)
    {
        Transform transform = Get(bone);
        return transform != null ? transform.rotation : Quaternion.identity;
    }

    public void ResetPose()
    {
        foreach (KeyValuePair<Transform, Quaternion> pair in baseLocalRotations)
        {
            if (pair.Key != null)
                pair.Key.localRotation = pair.Value;
        }

        foreach (KeyValuePair<Transform, Vector3> pair in baseLocalPositions)
        {
            if (pair.Key != null)
                pair.Key.localPosition = pair.Value;
        }
    }

    public void RotateWorld(
        HumanBodyBones bone,
        Vector3 worldAxis,
        float degrees)
    {
        Transform transform = Get(bone);

        if (transform == null || Mathf.Abs(degrees) < 0.0001f)
            return;

        if (worldAxis.sqrMagnitude < 0.0001f)
            return;

        transform.rotation =
            Quaternion.AngleAxis(degrees, worldAxis.normalized) *
            transform.rotation;
    }

    public void RotateLocal(
        HumanBodyBones bone,
        Vector3 localEuler)
    {
        Transform transform = Get(bone);

        if (transform == null)
            return;

        transform.localRotation =
            transform.localRotation * Quaternion.Euler(localEuler);
    }

    public void OffsetHips(Vector3 localOffset)
    {
        Transform hips = Get(HumanBodyBones.Hips);

        if (hips == null || !baseLocalPositions.TryGetValue(hips, out Vector3 rest))
            return;

        hips.localPosition = rest + localOffset;
    }

    /// <summary>
    /// Two-bone IK that points both arm segments at the target. This rig's
    /// bone local Y is not aligned with the limb, so the solve rotates each
    /// segment by the delta between its current child direction and the
    /// desired direction instead of assuming a bone axis.
    /// </summary>
    public void SolveTwoBoneIK(
        HumanBodyBones upper,
        HumanBodyBones lower,
        HumanBodyBones hand,
        Vector3 target,
        Vector3 pole,
        float weight,
        Vector3 handAimEuler)
    {
        Transform upperBone = Get(upper);
        Transform lowerBone = Get(lower);
        Transform handBone = Get(hand);

        if (upperBone == null || lowerBone == null || handBone == null)
            return;

        weight = Mathf.Clamp01(weight);

        if (weight <= 0.0001f)
            return;

        Vector3 shoulder = upperBone.position;
        float upperLength = Vector3.Distance(shoulder, lowerBone.position);
        float lowerLength = Vector3.Distance(
            lowerBone.position,
            handBone.position
        );

        if (upperLength < 0.001f || lowerLength < 0.001f)
            return;

        Vector3 toTarget = target - shoulder;
        float targetDistance = toTarget.magnitude;

        if (targetDistance < 0.001f)
            return;

        float maxLength = upperLength + lowerLength - 0.001f;
        float distance = Mathf.Clamp(targetDistance, 0.05f, maxLength);
        Vector3 direction = toTarget / targetDistance;

        float cosine = (
            upperLength * upperLength +
            distance * distance -
            lowerLength * lowerLength
        ) / (2f * upperLength * distance);

        float elbowAngle = Mathf.Acos(Mathf.Clamp(cosine, -1f, 1f));

        Vector3 poleDirection = pole - shoulder;
        poleDirection -= direction * Vector3.Dot(poleDirection, direction);

        if (poleDirection.sqrMagnitude < 0.0001f)
        {
            poleDirection = Vector3.Cross(
                direction,
                root != null ? root.up : Vector3.up
            );
        }

        poleDirection.Normalize();

        Vector3 elbow = shoulder + (
            direction * Mathf.Cos(elbowAngle) +
            poleDirection * Mathf.Sin(elbowAngle)
        ) * upperLength;

        Vector3 currentUpper = lowerBone.position - shoulder;

        if (currentUpper.sqrMagnitude > 0.0001f)
        {
            Quaternion upperTarget =
                Quaternion.FromToRotation(currentUpper, elbow - shoulder) *
                upperBone.rotation;

            upperBone.rotation = Quaternion.Slerp(
                upperBone.rotation,
                upperTarget,
                weight
            );
        }

        Vector3 currentLower = handBone.position - lowerBone.position;

        if (currentLower.sqrMagnitude > 0.0001f)
        {
            Quaternion lowerTarget =
                Quaternion.FromToRotation(
                    currentLower,
                    target - lowerBone.position
                ) *
                lowerBone.rotation;

            lowerBone.rotation = Quaternion.Slerp(
                lowerBone.rotation,
                lowerTarget,
                weight
            );
        }

        // A tiny authored wrist offset lets the pose be tuned without changing
        // the IK math. Zero by default keeps the hand aligned with the forearm.
        if (handAimEuler.sqrMagnitude > 0.0001f)
        {
            handBone.localRotation =
                handBone.localRotation * Quaternion.Euler(handAimEuler);
        }
    }
}
