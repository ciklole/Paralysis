#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot editor setup for the cashier transformation sequence. It wires the
/// authored Store scene to the runtime performers without rebuilding the map.
///
/// Menu: Paralysis > Setup > Build Cashier Sequence
/// Terminal: write "buildcashier" to Temp/urp_request.txt
/// </summary>
[NoAutoStaticsCleanup]
public static class CashierSequenceBuilder
{
    private const string StoreScenePath = "Assets/Scenes/Store.unity";
    private const string DumpPath = "Temp/checkout_dump.txt";

    [MenuItem("Paralysis/Setup/Dump Checkout Area")]
    public static void DumpCheckout()
    {
        DumpCheckoutInternal();
    }

    /// <summary>
    /// Wires the runtime performers into the authored Store scene. Kept
    /// separate from the dump so the diagnostic can run before the runtime
    /// scripts exist.
    /// </summary>
    [MenuItem("Paralysis/Setup/Build Cashier Sequence")]
    public static void Build()
    {
        Scene scene = EnsureStoreScene();
        CashierInteraction cashier = FindInScene<CashierInteraction>(scene);

        if (cashier == null)
        {
            Debug.LogError(
                "CashierSequenceBuilder: no CashierInteraction in " +
                StoreScenePath + "."
            );

            return;
        }

        SerializedObject cashierObject = new SerializedObject(cashier);
        GameObject normalModel =
            ResolveReference(cashierObject, "normalModel") as GameObject;
        GameObject transformedModel =
            ResolveReference(cashierObject, "transformedModel") as GameObject;

        if (normalModel == null || transformedModel == null)
        {
            Debug.LogError(
                "CashierSequenceBuilder: cashier is missing its normal or " +
                "transformed model reference."
            );

            return;
        }

        GameObject sequenceObject = FindRoot(scene, "Cashier Sequence");

        if (sequenceObject == null)
            sequenceObject = new GameObject("Cashier Sequence");

        sequenceObject.transform.SetPositionAndRotation(
            Vector3.zero,
            Quaternion.identity
        );

        CameraFocusController focus =
            EnsureComponent<CameraFocusController>(sequenceObject);
        StoreLightingController lighting =
            EnsureComponent<StoreLightingController>(sequenceObject);
        MonsterApproachController approach =
            EnsureComponent<MonsterApproachController>(sequenceObject);

        CashierAnimationDriver cashierAnimation =
            normalModel.GetComponent<CashierAnimationDriver>();

        if (cashierAnimation == null)
            cashierAnimation = normalModel.AddComponent<CashierAnimationDriver>();

        MonsterAnimationDriver monsterAnimation =
            transformedModel.GetComponent<MonsterAnimationDriver>();

        if (monsterAnimation == null)
            monsterAnimation = transformedModel.AddComponent<MonsterAnimationDriver>();

        Animator normalAnimator =
            normalModel.GetComponentInChildren<Animator>(true);
        Animator monsterAnimator =
            transformedModel.GetComponentInChildren<Animator>(true);
        Transform pumpkin = monsterAnimator != null
            ? monsterAnimator.transform
            : transformedModel.transform;

        SerializedObject cashierDriverObject =
            new SerializedObject(cashierAnimation);
        SetReference(cashierDriverObject, "animator", normalAnimator);
        SetFloat(cashierDriverObject, "armRestAngle", -1f);
        cashierDriverObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject monsterDriverObject =
            new SerializedObject(monsterAnimation);
        SetReference(monsterDriverObject, "animator", monsterAnimator);
        SetFloat(monsterDriverObject, "armRestAngle", -1f);
        SetFloat(monsterDriverObject, "handSpread", 0.2f);
        monsterDriverObject.ApplyModifiedPropertiesWithoutUndo();

        Transform dialogueAnchor = EnsureAnchor(
            sequenceObject.transform,
            "Camera Dialogue Anchor",
            new Vector3(6.37f, 1.95f, 0.25f)
        );

        Transform transformAnchor = EnsureAnchor(
            sequenceObject.transform,
            "Camera Transform Anchor",
            new Vector3(6.37f, 1.95f, 0.95f)
        );

        Transform approachAnchor = EnsureAnchor(
            sequenceObject.transform,
            "Camera Approach Anchor",
            new Vector3(6.37f, 1.95f, -2.2f)
        );

        Transform approachLookTarget = EnsureAnchor(
            sequenceObject.transform,
            "Approach Look Target",
            new Vector3(6.37f, 1.8f, 1.5f)
        );

        Transform reachAnchor = EnsureAnchor(
            sequenceObject.transform,
            "Camera Reach Anchor",
            new Vector3(6.58f, 2.04f, -0.72f)
        );

        Transform lookTarget = EnsureAnchor(
            sequenceObject.transform,
            "Cashier Look Target",
            new Vector3(6.37f, 2.30f, 2.71f)
        );

        Transform pathRoot = EnsureAnchor(
            sequenceObject.transform,
            "Monster Path",
            Vector3.zero
        );

        // The pumpkin's visual body is the child at (6.04, 0, 3.27), exactly
        // where the cashier stands. Following the pumpkin directly keeps the
        // body on the path instead of orbiting an offset root pivot.
        Vector3[] pathPositions =
        {
            new Vector3(6.04f, 0f, 3.27f),  // where the cashier was
            new Vector3(6.04f, 0f, 4.60f),  // step back behind the counter
            new Vector3(4.25f, 0f, 4.60f),  // around the west end
            new Vector3(4.25f, 0f, 1.50f),  // emerge into the open aisle
            new Vector3(5.40f, 0f, 1.00f),  // cut diagonally toward the player
            new Vector3(6.49f, 0f, 0.75f)   // in front of the camera
        };

        string[] pathNames =
        {
            "Monster Start",
            "Monster Behind Counter",
            "Monster West Turn",
            "Monster Emerges",
            "Monster Crosses Aisle",
            "Monster Reach Position"
        };

        Transform[] pathPoints = new Transform[pathPositions.Length];

        for (int i = 0; i < pathPositions.Length; i++)
        {
            pathPoints[i] = EnsureAnchor(
                pathRoot,
                pathNames[i],
                pathPositions[i]
            );
        }

        SerializedObject approachObject = new SerializedObject(approach);
        SetReference(approachObject, "monsterRoot", pumpkin);
        SetReference(approachObject, "animationDriver", monsterAnimation);
        SetReference(approachObject, "faceTarget", null);
        SetFloat(approachObject, "moveSpeed", 1.1f);
        SetFloat(approachObject, "acceleration", 1.1f);
        SetFloat(approachObject, "turnSpeed", 120f);
        SetFloat(approachObject, "waypointTolerance", 0.06f);
        SetFloat(approachObject, "finalSlowdownDistance", 1.4f);
        SetFloat(approachObject, "modelYawOffset", 0f);
        SetBool(approachObject, "snapToGround", false);

        SerializedProperty pathProperty =
            approachObject.FindProperty("pathPoints");

        if (pathProperty != null)
        {
            pathProperty.arraySize = pathPoints.Length;

            for (int i = 0; i < pathPoints.Length; i++)
            {
                pathProperty.GetArrayElementAtIndex(i).objectReferenceValue =
                    pathPoints[i];
            }
        }

        approachObject.ApplyModifiedPropertiesWithoutUndo();

        SetReference(cashierObject, "cameraFocus", focus);
        SetReference(cashierObject, "cameraDialogueAnchor", dialogueAnchor);
        SetReference(cashierObject, "cameraTransformAnchor", transformAnchor);
        SetReference(cashierObject, "cameraApproachAnchor", approachAnchor);
        SetReference(
            cashierObject,
            "cameraApproachLookTarget",
            approachLookTarget
        );
        SetReference(cashierObject, "cameraReachAnchor", reachAnchor);
        SetReference(cashierObject, "cashierLookTarget", lookTarget);
        SetReference(cashierObject, "lighting", lighting);
        SetReference(cashierObject, "cashierAnimation", cashierAnimation);
        SetReference(cashierObject, "monsterAnimation", monsterAnimation);
        SetReference(cashierObject, "monsterApproach", approach);

        SetFloat(cashierObject, "cameraDialogueDuration", 1.4f);
        SetFloat(cashierObject, "cameraTransformDuration", 2.4f);
        SetFloat(cashierObject, "cameraApproachDuration", 2f);
        SetFloat(cashierObject, "cameraReachDuration", 1.5f);
        SetFloat(cashierObject, "cameraReturnDuration", 1.4f);

        SerializedObject lightingObject = new SerializedObject(lighting);
        SetFloat(lightingObject, "redIntensityScale", 1.3f);
        SetColor(
            lightingObject,
            "redLightColor",
            new Color(1f, 0.045f, 0.02f, 1f)
        );
        SetColor(
            lightingObject,
            "redPanelEmission",
            new Color(3.2f, 0.03f, 0.015f, 1f)
        );
        lightingObject.ApplyModifiedPropertiesWithoutUndo();

        SetFloat(cashierObject, "lightingTransitionDuration", 2.6f);
        SetFloat(cashierObject, "lightingFlickerDuration", 1.4f);
        SetFloat(cashierObject, "restoreLightingDuration", 1.2f);

        SetFloat(cashierObject, "monsterRevealDelay", 0.45f);
        SetFloat(cashierObject, "monsterApproachTimeout", 20f);
        SetFloat(cashierObject, "reachDuration", 2.4f);
        SetFloat(cashierObject, "reachHoldDuration", 1.8f);

        SetFloat(cashierObject, "preTransformationDelay", 1.2f);
        SetFloat(cashierObject, "silencePhase", 0.6f);
        SetFloat(cashierObject, "visualPausePhase", 0.9f);
        SetFloat(cashierObject, "transformationPhase", 1.3f);

        cashierObject.ApplyModifiedPropertiesWithoutUndo();

        if (normalModel == transformedModel)
        {
            Debug.LogWarning(
                "CashierSequenceBuilder: normal and transformed models are " +
                "the same object; the swap will not be visible."
            );
        }

        if (!normalModel.activeSelf)
            normalModel.SetActive(true);

        if (transformedModel.activeSelf)
            transformedModel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "CashierSequenceBuilder: wired cashier sequence at " +
            PathOf(sequenceObject.transform) +
            " | pumpkin=" + PathOf(pumpkin) +
            " | path=" + pathPoints.Length +
            " | normalAnimator=" +
            (normalAnimator != null ? normalAnimator.name : "null") +
            " | monsterAnimator=" +
            (monsterAnimator != null ? monsterAnimator.name : "null")
        );
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static Transform EnsureAnchor(
        Transform parent,
        string name,
        Vector3 worldPosition)
    {
        Transform anchor = parent.Find(name);

        if (anchor == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            anchor = go.transform;
        }

        anchor.position = worldPosition;
        anchor.rotation = Quaternion.identity;
        return anchor;
    }

    private static void SetReference(
        SerializedObject so,
        string name,
        UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property == null)
        {
            Debug.LogWarning(
                "CashierSequenceBuilder: missing serialized field '" +
                name + "' on " + so.targetObject.GetType().Name + "."
            );

            return;
        }

        property.objectReferenceValue = value;
    }

    private static void SetFloat(
        SerializedObject so,
        string name,
        float value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
            property.floatValue = value;
    }

    private static void SetBool(
        SerializedObject so,
        string name,
        bool value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
            property.boolValue = value;
    }

    private static void SetColor(
        SerializedObject so,
        string name,
        Color value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
            property.colorValue = value;
    }

    [MenuItem("Paralysis/Setup/Dump Character Bones")]
    public static void DumpBones()
    {
        Scene scene = EnsureStoreScene();
        StringBuilder report = new StringBuilder();

        CashierInteraction cashier = FindInScene<CashierInteraction>(scene);

        if (cashier == null)
        {
            report.AppendLine("CashierInteraction not found.");
            WriteText("Temp/character_bones.txt", report);
            return;
        }

        SerializedObject so = new SerializedObject(cashier);
        GameObject normalObject = ResolveReference(so, "normalModel") as GameObject;
        GameObject transformedObject = ResolveReference(so, "transformedModel") as GameObject;
        Transform normal = normalObject != null ? normalObject.transform : null;
        Transform transformed = transformedObject != null
            ? transformedObject.transform
            : null;

        DumpRig(report, "NORMAL", normal);
        report.AppendLine();
        DumpRig(report, "MONSTER", transformed);

        WriteText("Temp/character_bones.txt", report);
        Debug.Log("CashierSequenceBuilder: wrote Temp/character_bones.txt");
    }

    private static void DumpRig(StringBuilder report, string label, Transform root)
    {
        report.AppendLine("=== " + label + " ===");

        if (root == null)
        {
            report.AppendLine("root: null");
            return;
        }

        report.AppendLine(
            "root: " + PathOf(root) +
            " | pos=" + root.position +
            " | rot=" + root.eulerAngles +
            " | scale=" + root.lossyScale
        );

        Animator animator = root.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            report.AppendLine("animator: null");
            return;
        }

        report.AppendLine(
            "animator: " + PathOf(animator.transform) +
            " | avatar=" + (animator.avatar != null ? animator.avatar.name : "null") +
            " | controller=" + (animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "null")
        );

        HumanBodyBones[] bones =
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

        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = animator.GetBoneTransform(bones[i]);

            if (bone == null)
            {
                report.AppendLine(bones[i] + ": null");
                continue;
            }

            report.AppendLine(
                bones[i] +
                " | path=" + PathOf(bone) +
                " | pos=" + bone.position +
                " | localPos=" + bone.localPosition +
                " | localRot=" + bone.localRotation.eulerAngles +
                " | worldRot=" + bone.rotation.eulerAngles
            );
        }

        report.AppendLine("--- arm axes ---");
        LogBoneAxis(report, animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm);
        LogBoneAxis(report, animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
        LogBoneAxis(report, animator, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        LogBoneAxis(report, animator, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
        LogBoneAxis(report, animator, HumanBodyBones.Neck, HumanBodyBones.Head);
    }

    private static void LogBoneAxis(
        StringBuilder report,
        Animator animator,
        HumanBodyBones bone,
        HumanBodyBones child)
    {
        Transform a = animator.GetBoneTransform(bone);
        Transform b = animator.GetBoneTransform(child);

        if (a == null || b == null)
            return;

        Vector3 dir = (b.position - a.position).normalized;

        report.AppendLine(
            bone + " -> " + child +
            " | worldDir=" + dir +
            " | localY=" + a.TransformDirection(Vector3.up) +
            " | localZ=" + a.TransformDirection(Vector3.forward) +
            " | localX=" + a.TransformDirection(Vector3.right)
        );
    }

    private static void DumpCheckoutInternal()
    {
        Scene scene = EnsureStoreScene();
        StringBuilder report = new StringBuilder();

        report.AppendLine("=== CHECKOUT AREA DUMP ===");
        report.AppendLine("Scene: " + scene.path);

        CashierInteraction cashier = FindInScene<CashierInteraction>(scene);

        if (cashier == null)
        {
            report.AppendLine("CashierInteraction not found.");
            WriteDump(report);
            return;
        }

        report.AppendLine();
        report.AppendLine("--- Cashier ---");
        report.AppendLine("Path: " + PathOf(cashier.transform));
        report.AppendLine("Position: " + cashier.transform.position);
        report.AppendLine("Rotation: " + cashier.transform.eulerAngles);
        report.AppendLine("Scale: " + cashier.transform.lossyScale);
        report.AppendLine("Interaction point: " + cashier.InteractionPoint.position);

        SerializedObject so = new SerializedObject(cashier);
        LogObjectReference(report, so, "normalModel");
        LogObjectReference(report, so, "transformedModel");
        LogObjectReference(report, so, "animator");

        Transform normal = ResolveReference(so, "normalModel") as Transform;
        Transform transformed = ResolveReference(so, "transformedModel") as Transform;

        if (normal != null)
        {
            report.AppendLine();
            report.AppendLine("--- Normal Model ---");
            DumpTransformTree(report, normal, 0, 4);
        }

        if (transformed != null)
        {
            report.AppendLine();
            report.AppendLine("--- Transformed Model ---");
            DumpTransformTree(report, transformed, 0, 4);
        }

        report.AppendLine();
        report.AppendLine("--- Lights ---");

        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Array.Sort(lights, (a, b) => string.Compare(
            PathOf(a.transform),
            PathOf(b.transform),
            StringComparison.OrdinalIgnoreCase
        ));

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];

            report.AppendLine(
                PathOf(light.transform) +
                " | type=" + light.type +
                " | enabled=" + light.enabled +
                " | active=" + light.gameObject.activeInHierarchy +
                " | color=" + light.color +
                " | intensity=" + light.intensity +
                " | range=" + light.range +
                " | spot=" + light.spotAngle +
                " | inner=" + light.innerSpotAngle +
                " | shadows=" + light.shadows
            );
        }

        report.AppendLine();
        report.AppendLine("--- Light-ish renderers ---");

        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material[] materials = renderer.sharedMaterials;

            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];

                if (material == null)
                    continue;

                string name = material.name.ToLowerInvariant();

                if (!name.Contains("light") && !name.Contains("lamp") &&
                    !name.Contains("neon") && !name.Contains("sign"))
                {
                    continue;
                }

                report.AppendLine(
                    PathOf(renderer.transform) +
                    " | material=" + material.name +
                    " | shader=" + material.shader.name +
                    " | emission=" + SafeEmission(material)
                );
            }
        }

        report.AppendLine();
        report.AppendLine("--- Colliders near checkout ---");

        Vector3 centre = cashier.transform.position;
        Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        List<Collider> nearby = new List<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null)
                continue;

            float distance = Vector3.Distance(centre, collider.bounds.center);

            if (distance <= 14f)
                nearby.Add(collider);
        }

        nearby.Sort((a, b) => Vector3.Distance(centre, a.bounds.center)
            .CompareTo(Vector3.Distance(centre, b.bounds.center)));

        for (int i = 0; i < nearby.Count; i++)
        {
            Collider collider = nearby[i];

            report.AppendLine(
                PathOf(collider.transform) +
                " | type=" + collider.GetType().Name +
                " | trigger=" + collider.isTrigger +
                " | boundsCenter=" + collider.bounds.center +
                " | boundsSize=" + collider.bounds.size
            );
        }

        WriteDump(report);
        Debug.Log("CashierSequenceBuilder: wrote " + DumpPath);
    }

    private static Scene EnsureStoreScene()
    {
        Scene active = SceneManager.GetActiveScene();

        if (active.IsValid() && active.path == StoreScenePath)
            return active;

        return EditorSceneManager.OpenScene(
            StoreScenePath,
            OpenSceneMode.Single
        );
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            T found = roots[i].GetComponentInChildren<T>(true);

            if (found != null)
                return found;
        }

        return null;
    }

    private static UnityEngine.Object ResolveReference(
        SerializedObject so,
        string propertyName)
    {
        SerializedProperty property = so.FindProperty(propertyName);

        return property != null ? property.objectReferenceValue : null;
    }

    private static void LogObjectReference(
        StringBuilder report,
        SerializedObject so,
        string propertyName)
    {
        UnityEngine.Object value = ResolveReference(so, propertyName);
        string description = value == null ? "null" : value.name;

        if (value is Component component)
            description += " (" + PathOf(component.transform) + ")";

        report.AppendLine(propertyName + ": " + description);
    }

    private static string SafeEmission(Material material)
    {
        if (material.HasProperty("_EmissionColor"))
        {
            Color emission = material.GetColor("_EmissionColor");
            bool keyword = material.IsKeywordEnabled("_EMISSION");

            return emission + " keyword=" + keyword;
        }

        return "none";
    }

    private static void DumpTransformTree(
        StringBuilder report,
        Transform root,
        int depth,
        int maxDepth)
    {
        report.AppendLine(
            new string(' ', depth * 2) +
            root.name +
            " | pos=" + root.localPosition +
            " | rot=" + root.localRotation.eulerAngles +
            " | scale=" + root.localScale
        );

        if (depth >= maxDepth)
        {
            if (root.childCount > 0)
            {
                report.AppendLine(
                    new string(' ', (depth + 1) * 2) + "... " +
                    root.childCount + " children"
                );
            }

            return;
        }

        for (int i = 0; i < root.childCount; i++)
            DumpTransformTree(report, root.GetChild(i), depth + 1, maxDepth);
    }

    private static string PathOf(Transform transform)
    {
        List<string> parts = new List<string>();

        while (transform != null)
        {
            parts.Add(transform.name);
            transform = transform.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void WriteDump(StringBuilder report)
    {
        WriteText(DumpPath, report);
    }

    private static void WriteText(string relativePath, StringBuilder report)
    {
        string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
        string path = Path.Combine(root, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, report.ToString());
    }
}
#endif
