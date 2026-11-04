using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A light switch the player can click from a distance.
/// Each click toggles ALL connected lights on/off and
/// automatically shows a hint in the bottom-right corner
/// when the switch can be pressed.
/// </summary>
public class LightSwitch : MonoBehaviour
{
    public enum MouseButton
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    [Header("Lights")]
    [Tooltip("Every light this switch controls - add as many as you want with the + button")]
    [SerializeField] private List<GameObject> lightObjects = new List<GameObject>();

    [Tooltip("Should the lights already be on when the game starts?")]
    [SerializeField] private bool startLightOn = true;

    [Header("Interaction")]
    [Tooltip("Which mouse button presses the switch")]
    [SerializeField] private MouseButton mouseButton = MouseButton.Left;

    [Tooltip("Max distance (in meters) from the player to the switch")]
    [SerializeField] private float interactionRange = 5f;

    [Tooltip("Objects that can block the click, like walls (Everything by default)")]
    [SerializeField] private LayerMask clickMask = ~0;

    [Header("UI Hint (bottom-right corner)")]
    [Tooltip("Text shown in the bottom-right corner when the switch can be pressed")]
    [SerializeField] private string hintText = "Click to toggle light";

    [Tooltip("Font for the hint (leave empty to use the built-in font)")]
    [SerializeField] private Font hintFont;

    [Tooltip("Size of the hint text")]
    [SerializeField] private int hintFontSize = 24;

    [Tooltip("Render the hint in bold")]
    [SerializeField] private bool hintBold = true;

    [Tooltip("Color of the hint text")]
    [SerializeField] private Color hintColor = Color.white;

    private Camera playerCamera;
    private GameObject hintObject;
    private Text hintTextComponent;
    private bool lightsAreOn;
    private bool wasBlocked;

    /// <summary>True while this switch's lights are on. Read by LightsObjective.</summary>
    public bool IsOn => lightsAreOn;

    private void Start()
    {
        CreateHintUI();

        if (!HasAnyLight())
        {
            Debug.LogWarning("LightSwitch: no lights assigned! Add lights to the 'Light Objects' list.", this);
            return;
        }

        lightsAreOn = startLightOn;
        ApplyLightState(lightsAreOn);
    }

    /// <summary>Builds the bottom-right hint automatically at startup.</summary>
    private void CreateHintUI()
    {
        // Full screen overlay canvas
        GameObject canvasObject = new GameObject("LightSwitchCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        // Hint text
        hintObject = new GameObject("LightSwitchHint");
        hintObject.transform.SetParent(canvasObject.transform, false);

        hintTextComponent = hintObject.AddComponent<Text>();
        hintTextComponent.font = hintFont != null ? hintFont : GetDefaultFont();
        hintTextComponent.fontSize = hintFontSize;
        hintTextComponent.fontStyle = hintBold ? FontStyle.Bold : FontStyle.Normal;
        hintTextComponent.color = hintColor;
        hintTextComponent.text = hintText;
        hintTextComponent.alignment = TextAnchor.LowerRight;
        hintTextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        hintTextComponent.verticalOverflow = VerticalWrapMode.Overflow;

        // Anchor to the bottom-right corner with a small margin
        RectTransform rt = hintObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 40f);
        rt.sizeDelta = new Vector2(600f, 80f);

        // Hidden until the player looks at the switch
        hintObject.SetActive(false);
    }

    private static Font GetDefaultFont()
    {
        return GameFont.Default;
    }

    private void Update()
    {
        // A dialogue, an inspection or the mission log is using the screen.
        if (UIState.BlocksGameplay)
        {
            wasBlocked = true;
            SetHintVisible(false);
            return;
        }

        // The click that closes a dialogue must not also toggle a switch.
        if (wasBlocked)
        {
            wasBlocked = false;
            SetHintVisible(false);
            return;
        }

        // Show the hint only when the switch is actually pressable:
        // has lights, aimed at, in range and nothing blocking the view
        bool canInteract = HasAnyLight() && RaycastHitsSwitch();
        SetHintVisible(canInteract);

        if (canInteract && Input.GetMouseButtonDown((int)mouseButton))
        {
            // Toggle every connected light at once
            lightsAreOn = !lightsAreOn;
            ApplyLightState(lightsAreOn);
        }
    }

    /// <summary>True if at least one real light is assigned in the list.</summary>
    private bool HasAnyLight()
    {
        if (lightObjects == null)
            return false;

        for (int i = 0; i < lightObjects.Count; i++)
        {
            if (lightObjects[i] != null)
                return true;
        }

        return false;
    }

    /// <summary>Turns every assigned light on or off (empty list slots are skipped).</summary>
    private void ApplyLightState(bool on)
    {
        if (lightObjects == null)
            return;

        for (int i = 0; i < lightObjects.Count; i++)
        {
            if (lightObjects[i] != null)
                lightObjects[i].SetActive(on);
        }
    }

    private bool RaycastHitsSwitch()
    {
        Camera cam = GetPlayerCamera();
        if (cam == null)
            return false;

        // Shoot a ray from the camera through the cursor
        // (cursor is locked, so this aims at the crosshair)
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // The ray can only reach interactionRange meters,
        // so the player must be close enough to press the switch
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, clickMask, QueryTriggerInteraction.Collide))
        {
            // Is the hit this switch (or one of its children)?
            return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
        }

        return false;
    }

    private void SetHintVisible(bool visible)
    {
        if (hintObject != null && hintObject.activeSelf != visible)
        {
            hintObject.SetActive(visible);
        }
    }

    private Camera GetPlayerCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindAnyObjectByType<Camera>();
        }

        return playerCamera;
    }

    // Shows the interaction range around the switch in the Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
