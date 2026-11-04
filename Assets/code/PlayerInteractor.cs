using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the F / E interaction keys. Every frame it picks exactly one valid
/// target, shows a single shared prompt and forwards the key press to that
/// target only. Replaces the per-object key listeners.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance;

    [Header("Selection")]
    [Tooltip("How far the target may be from the centre of the screen.")]
    [SerializeField] private float maxViewAngle = 40f;

    [Tooltip("Absolute maximum reach, regardless of the target's own range.")]
    [SerializeField] private float maxRange = 7f;

    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask blockingMask = ~0;

    [Header("Prompt")]
    [SerializeField] private string promptFormat = "{0}";
    [SerializeField] private TextStyle promptStyle = new TextStyle
    {
        fontSize = 24,
        color = Color.white
    };

    [SerializeField] private Color promptBackground = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Vector2 promptAnchor = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, -60f);
    [SerializeField] private Vector2 promptSize = new Vector2(560f, 54f);

    [Header("Outline")]
    [SerializeField] private bool enableOutline = true;

    private Camera cam;
    private Canvas canvas;
    private RectTransform promptPanel;
    private Text promptText;
    private IInteractable current;
    private InteractionOutline outline;
    private InteractableObject outlinedTarget;
    private bool wasBlocked;
    private bool visible;
    private bool suspended;

    public static PlayerInteractor Ensure()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("PlayerInteractor");
        return go.AddComponent<PlayerInteractor>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy is deferred to the end of the frame. Deactivate first so
            // the duplicate cannot create UI, subscribe, or leave a modal/lock
            // behind during that frame.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Scenes saved before the prompt was moved to the crosshair still
        // have the old 32%-height anchor. Upgrade that exact default once.
        if (Mathf.Abs(promptAnchor.x - 0.5f) < 0.001f &&
            Mathf.Abs(promptAnchor.y - 0.32f) < 0.001f)
        {
            promptAnchor = new Vector2(0.5f, 0.5f);

            if (promptOffset == Vector2.zero)
                promptOffset = new Vector2(0f, -60f);
        }

        CreatePromptUI();

        outline = GetComponent<InteractionOutline>();

        if (outline == null)
            outline = gameObject.AddComponent<InteractionOutline>();
    }

    private void OnDisable()
    {
        current = null;
        ClearOutline();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (outline != null)
            outline.Hide();

        if (Instance == this)
            Instance = null;
    }

    public bool IsSuspended => suspended;

    /// <summary>
    /// Temporarily stops prompts and interaction without disabling this
    /// persistent component (the driving scene uses this).
    /// </summary>
    public void SetSuspended(bool value)
    {
        suspended = value;

        if (!suspended)
            return;

        current = null;
        UpdateOutline(null);
        SetVisible(false);
    }

    private void CreatePromptUI()
    {
        canvas = UIHelper.CreateCanvas("InteractionPromptCanvas", 260, false);
        canvas.transform.SetParent(transform, false);

        promptPanel = UIHelper.CreateRect(
            "PromptPanel",
            canvas.transform,
            promptAnchor,
            new Vector2(0.5f, 0.5f),
            promptOffset,
            promptSize
        );

        UIHelper.AddPanel(promptPanel, promptBackground);

        RectTransform textRect = UIHelper.CreateStretchRect(
            "PromptText",
            promptPanel,
            new Vector2(16f, 6f),
            new Vector2(-16f, -6f)
        );

        promptText = UIHelper.AddText(textRect, promptStyle, string.Empty);

        promptPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (suspended)
        {
            current = null;
            UpdateOutline(null);
            SetVisible(false);
            return;
        }

        if (UIState.BlocksGameplay)
        {
            wasBlocked = true;
            current = null;
            UpdateOutline(null);
            SetVisible(false);
            return;
        }

        // The frame a modal closes, the same key press is still down. Skip it
        // so closing an inspection cannot instantly re-open it.
        if (wasBlocked)
        {
            wasBlocked = false;
            current = null;
            UpdateOutline(null);
            SetVisible(false);
            return;
        }

        if (cam == null)
            cam = InteractionUtil.FindCamera();

        if (cam == null)
        {
            current = null;
            UpdateOutline(null);
            SetVisible(false);
            return;
        }

        current = FindBest();
        UpdateOutline(current);

        if (current == null)
        {
            SetVisible(false);
            return;
        }

        ShowPrompt(current.InteractionPrompt);

        if (Input.GetKeyDown(current.InteractionKey))
            current.Interact();
    }

    private IInteractable FindBest()
    {
        IInteractable best = null;
        float bestScore = float.MaxValue;

        IReadOnlyList<IInteractable> list = InteractionRegistry.Items;
        Vector3 origin = cam.transform.position;
        Vector3 forward = cam.transform.forward;

        for (int i = 0; i < list.Count; i++)
        {
            IInteractable item = list[i];
            Component unityComponent = item as Component;

            if (unityComponent == null)
                continue;

            if (!item.CanInteract)
                continue;

            Transform point = item.InteractionPoint != null
                ? item.InteractionPoint
                : unityComponent.transform;

            Vector3 toTarget = point.position - origin;
            float distance = toTarget.magnitude;

            if (distance > maxRange || distance > item.InteractionRange)
                continue;

            if (distance > 0.01f &&
                Vector3.Angle(forward, toTarget) > maxViewAngle)
            {
                continue;
            }

            if (requireLineOfSight &&
                IsBlocked(origin, point.position, unityComponent.transform))
            {
                continue;
            }

            // Higher priority first, then closer.
            float score = distance - item.InteractionPriority * 1000f;

            if (score < bestScore)
            {
                bestScore = score;
                best = item;
            }
        }

        return best;
    }

    private bool IsBlocked(
        Vector3 origin,
        Vector3 point,
        Transform target)
    {
        if (!Physics.Linecast(
                origin,
                point,
                out RaycastHit hit,
                blockingMask,
                QueryTriggerInteraction.Collide))
        {
            return false;
        }

        return hit.collider.transform != target &&
               !hit.collider.transform.IsChildOf(target);
    }

    private void ShowPrompt(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            SetVisible(false);
            return;
        }

        promptText.text = string.Format(promptFormat, message);
        SetVisible(true);
    }

    private void UpdateOutline(IInteractable target)
    {
        InteractableObject interactable = enableOutline
            ? target as InteractableObject
            : null;

        if (interactable == null || !interactable.ShowOutline)
        {
            ClearOutline();
            return;
        }

        if (outlinedTarget == interactable)
            return;

        outlinedTarget = interactable;

        if (outline != null)
        {
            outline.Show(
                interactable.OutlineRoot,
                interactable.OutlineColor,
                interactable.OutlineWidth
            );
        }
    }

    private void ClearOutline()
    {
        if (outlinedTarget == null)
            return;

        outlinedTarget = null;

        if (outline != null)
            outline.Hide();
    }

    private void SetVisible(bool value)
    {
        if (visible == value || promptPanel == null)
            return;

        visible = value;
        promptPanel.gameObject.SetActive(value);
    }
}
