using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared text styling used by every UI in this project. Change the font,
/// size, colour and alignment per component in the Inspector.
/// </summary>
[System.Serializable]
public class TextStyle
{
    public Font font;
    public int fontSize = 28;
    public bool bold = true;
    public Color color = Color.white;
    public TextAnchor alignment = TextAnchor.MiddleCenter;

    public Font ResolveFont()
    {
        if (font != null)
            return font;

        return GameFont.Default;
    }

    public void Apply(Text text)
    {
        text.font = ResolveFont();
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.raycastTarget = false;
    }
}

/// <summary>
/// A stack of things that currently own the screen (dialogue, inspection,
/// mission log). Systems that read input check <see cref="BlocksGameplay"/>
/// instead of keeping their own booleans, so one system closing cannot
/// accidentally unlock the game while another is still open.
/// </summary>
public static class UIState
{
    private static readonly List<object> modals = new List<object>();

    public static bool BlocksGameplay => modals.Count > 0;

    /// <summary>
    /// The most recently pushed modal. A window that has been covered by
    /// another one (for example dialogue under the pause menu) can use this to
    /// ignore keyboard/mouse input until it is on top again.
    /// </summary>
    public static object Top =>
        modals.Count > 0 ? modals[modals.Count - 1] : null;

    public static void PushModal(object owner)
    {
        if (owner == null)
            return;

        if (!modals.Contains(owner))
            modals.Add(owner);
    }

    public static void PopModal(object owner)
    {
        if (owner == null)
            return;

        modals.Remove(owner);
    }

    public static bool IsModal(object owner)
    {
        return owner != null && modals.Contains(owner);
    }

    public static void ClearModals()
    {
        modals.Clear();
    }
}

/// <summary>
/// Builds canvases, panels and text at runtime so nothing has to be set up
/// by hand in the scene.
/// </summary>
public static class UIHelper
{
    public static readonly Vector2 ReferenceResolution =
        new Vector2(1920f, 1080f);

    public static Canvas CreateCanvas(
        string name,
        int sortingOrder,
        bool persistent)
    {
        EnsureEventSystem();

        GameObject go = new GameObject(name);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        if (persistent)
            Object.DontDestroyOnLoad(go);

        return canvas;
    }

    /// <summary>Creates an EventSystem once, for any future clickable UI.</summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();

        Object.DontDestroyOnLoad(go);
    }

    /// <summary>Creates a child rect anchored to a single point on its parent.</summary>
    public static RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return rect;
    }

    /// <summary>Creates a stretched child rect that fills its parent.</summary>
    public static RectTransform CreateStretchRect(
        string name,
        Transform parent,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        return rect;
    }

    public static Image AddPanel(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    public static Text AddText(
        RectTransform rect,
        TextStyle style,
        string content)
    {
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = content;
        style.Apply(text);

        return text;
    }

    /// <summary>
    /// Clips a rect so child text can never spill outside the panel.
    /// </summary>
    public static RectMask2D AddMask(RectTransform rect)
    {
        return rect.gameObject.AddComponent<RectMask2D>();
    }
}
