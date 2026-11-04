#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a manually resized button at the size you set, even when its parent
/// uses a layout group. A layout group normally overwrites a child's
/// RectTransform size on every rebuild, which makes hand-made size changes
/// snap back. When a button is resized in the Canvas, this copies the new size
/// into the button's LayoutElement, so the layout uses the size you chose.
/// </summary>
[InitializeOnLoad]
public static class ButtonSizeKeeper
{
    private static readonly HashSet<LayoutElement> pending =
        new HashSet<LayoutElement>();

    private static bool flushQueued;

    static ButtonSizeKeeper()
    {
        Undo.postprocessModifications += OnPostprocessModifications;
    }

    private static UndoPropertyModification[] OnPostprocessModifications(
        UndoPropertyModification[] modifications)
    {
        // Nothing to sync while the game is running; runtime builders own
        // their own sizes.
        if (Application.isPlaying)
            return modifications;

        for (int i = 0; i < modifications.Length; i++)
        {
            PropertyModification current = modifications[i].currentValue;

            if (current == null ||
                !(current.target is RectTransform rect) ||
                rect.GetComponent<Selectable>() == null)
            {
                continue;
            }

            string path = current.propertyPath;

            if (path != "m_SizeDelta.x" && path != "m_SizeDelta.y")
                continue;

            if (!IsUnderLayoutGroup(rect))
                continue;

            // Layout-controlled buttons normally already have a
            // LayoutElement. When one is missing, use
            // "Paralysis/UI/Keep Selected Button Sizes" to add it.
            LayoutElement layout = rect.GetComponent<LayoutElement>();

            if (layout == null)
                continue;

            if (!float.TryParse(
                    current.value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                continue;
            }

            Queue(layout);
        }

        return modifications;
    }

    /// <summary>
    /// Queues the sync for after the current undo event finishes. Recording
    /// another undo entry inside the callback would corrupt the undo stack.
    /// </summary>
    private static void Queue(LayoutElement layout)
    {
        pending.Add(layout);

        if (flushQueued)
            return;

        flushQueued = true;
        EditorApplication.delayCall += Flush;
    }

    private static void Flush()
    {
        flushQueued = false;

        foreach (LayoutElement layout in pending)
        {
            if (layout == null)
                continue;

            RectTransform rect = layout.transform as RectTransform;

            if (rect == null)
                continue;

            ApplySize(layout, rect.sizeDelta);
        }

        pending.Clear();
    }

    /// <summary>
    /// Adds a LayoutElement when needed and copies the button's current size
    /// into it. Use this for a button that did not have a LayoutElement yet.
    /// </summary>
    [MenuItem("Paralysis/UI/Keep Selected Button Sizes")]
    private static void KeepSelectedButtonSizes()
    {
        foreach (GameObject selected in Selection.gameObjects)
        {
            RectTransform rect = selected.GetComponent<RectTransform>();

            if (rect == null || rect.GetComponent<Selectable>() == null)
                continue;

            LayoutElement layout = rect.GetComponent<LayoutElement>();

            if (layout == null)
                layout = Undo.AddComponent<LayoutElement>(rect.gameObject);

            ApplySize(layout, rect.sizeDelta);
        }
    }

    private static void ApplySize(LayoutElement layout, Vector2 size)
    {
        float width = Mathf.Max(0f, size.x);
        float height = Mathf.Max(0f, size.y);

        if (Mathf.Approximately(layout.preferredWidth, width) &&
            Mathf.Approximately(layout.preferredHeight, height) &&
            Mathf.Approximately(layout.minWidth, width) &&
            Mathf.Approximately(layout.minHeight, height))
        {
            return;
        }

        Undo.RecordObject(layout, "Keep Button Size");

        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.minWidth = width;
        layout.minHeight = height;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        EditorUtility.SetDirty(layout);

        if (PrefabUtility.IsPartOfPrefabInstance(layout))
            PrefabUtility.RecordPrefabInstancePropertyModifications(layout);
    }

    private static bool IsUnderLayoutGroup(RectTransform rect)
    {
        Transform parent = rect.parent;

        while (parent != null)
        {
            if (parent.GetComponent<LayoutGroup>() != null)
                return true;

            parent = parent.parent;
        }

        return false;
    }
}
#endif
