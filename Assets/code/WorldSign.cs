using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates world space signs with the project font.
///
/// The signs used to be legacy TextMesh objects. The built-in font shader
/// that TextMesh relies on does not work under URP and turned pink, so the
/// signs now use a world space canvas with a regular UI Text, which renders
/// correctly in URP and still uses the electronic highway sign font.
/// </summary>
public static class WorldSign
{
    /// <summary>Canvas pixels per world meter.</summary>
    public const float PixelsPerMeter = 100f;

    /// <summary>
    /// Creates a sign that faces the local +Z axis of its parent.
    /// </summary>
    public static GameObject Create(
        string name,
        Transform parent,
        string message,
        Color color,
        float widthMeters,
        float heightMeters,
        int fontSize,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = localRotation;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = root.transform as RectTransform;

        if (canvasRect == null)
            canvasRect = root.AddComponent<RectTransform>();

        canvasRect.sizeDelta = new Vector2(
            widthMeters * PixelsPerMeter,
            heightMeters * PixelsPerMeter
        );

        canvasRect.localScale = Vector3.one * (1f / PixelsPerMeter);

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.AddComponent<Text>();
        text.font = GameFont.Default;
        text.text = message;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;

        return root;
    }
}
