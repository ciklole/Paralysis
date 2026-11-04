#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector helpers for the cashier's normal and transformed models. The
/// fields can always be assigned by hand; these buttons speed up setup and
/// catch the common "model is the component object" mistake.
/// </summary>
[CustomEditor(typeof(CashierInteraction))]
public class CashierInteractionEditor : Editor
{
    private SerializedProperty normalModel;
    private SerializedProperty transformedModel;
    private SerializedProperty animator;

    private void OnEnable()
    {
        normalModel = serializedObject.FindProperty("normalModel");
        transformedModel = serializedObject.FindProperty("transformedModel");
        animator = serializedObject.FindProperty("animator");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Model Helpers", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto-assign from children"))
                AutoAssignModels();

            if (GUILayout.Button("Validate setup"))
                ValidateSetup();
        }

        EditorGUILayout.HelpBox(
            "Assign child models, not the object that holds CashierInteraction. " +
            "The transformed model is optional.",
            MessageType.None
        );
    }

    private void AutoAssignModels()
    {
        CashierInteraction cashier = (CashierInteraction)target;
        Transform root = cashier.transform;

        GameObject normal = FindModel(
            root,
            "normal",
            "human",
            "cashier",
            "clerk"
        );
        GameObject transformed = FindModel(
            root,
            "scary",
            "transformed",
            "creepy",
            "monster"
        );

        if (normal == null && transformed == null)
        {
            // Fall back to the direct children with renderers. This is a
            // common layout: one ordinary model and one scary model.
            int rendererChildren = 0;
            GameObject first = null;
            GameObject second = null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.GetComponentInChildren<Renderer>(true) == null)
                    continue;

                rendererChildren++;

                if (first == null)
                    first = child.gameObject;
                else if (second == null)
                    second = child.gameObject;
            }

            if (rendererChildren == 1)
                normal = first;
            else if (rendererChildren == 2)
            {
                normal = first;
                transformed = second;
            }
        }

        Animator foundAnimator = cashier.GetComponentInChildren<Animator>(true);

        serializedObject.Update();

        if (normal != null && normalModel != null)
            normalModel.objectReferenceValue = normal;

        if (transformed != null && transformedModel != null)
            transformedModel.objectReferenceValue = transformed;

        if (foundAnimator != null && animator != null)
            animator.objectReferenceValue = foundAnimator;

        serializedObject.ApplyModifiedProperties();

        if (normal == null && transformed == null)
        {
            EditorUtility.DisplayDialog(
                "Cashier models",
                "No child models were found. Assign Normal Model and " +
                "Transformed Model by hand.",
                "OK"
            );
        }
    }

    private static GameObject FindModel(Transform root, params string[] keys)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == root)
                continue;

            string lower = child.name.ToLowerInvariant();

            for (int k = 0; k < keys.Length; k++)
            {
                if (lower.Contains(keys[k]))
                    return child.gameObject;
            }
        }

        return null;
    }

    private void ValidateSetup()
    {
        CashierInteraction cashier = (CashierInteraction)target;
        GameObject normal = normalModel != null
            ? normalModel.objectReferenceValue as GameObject
            : null;
        GameObject transformed = transformedModel != null
            ? transformedModel.objectReferenceValue as GameObject
            : null;
        Animator foundAnimator = animator != null
            ? animator.objectReferenceValue as Animator
            : null;

        string message = string.Empty;
        MessageType type = MessageType.Info;

        if (normal == null)
        {
            message += "Normal Model is not assigned. ";
            type = MessageType.Warning;
        }
        else if (normal == cashier.gameObject)
        {
            message += "Normal Model must be a child, not this object. ";
            type = MessageType.Error;
        }

        if (transformed != null && transformed == cashier.gameObject)
        {
            message += "Transformed Model must be a child, not this object. ";
            type = MessageType.Error;
        }

        if (normal != null && normal == transformed)
        {
            message += "Normal and transformed models are the same object. ";
            type = MessageType.Warning;
        }

        if (foundAnimator == null)
        {
            message += "No Animator is assigned; animation triggers will be " +
                       "skipped. ";
            type = type == MessageType.Error
                ? MessageType.Error
                : MessageType.Warning;
        }

        if (message.Length == 0)
            message = "Cashier model setup looks valid.";

        EditorUtility.DisplayDialog("Cashier setup", message.Trim(), "OK");
    }
}
#endif
