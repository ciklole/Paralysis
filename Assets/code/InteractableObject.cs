using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Press F while looking at this object: the camera slides in to a chosen
/// angle / distance, a description panel appears and optional sounds play.
/// Press F again (or Escape) and everything returns exactly as it was.
///
/// The panel is always in the same place on screen; only the fonts, colours
/// and camera framing change per object.
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private KeyCode exitKey = KeyCode.F;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private string prompt = "F - Inspect";
    [SerializeField] private int priority;
    [SerializeField] private Transform interactionPoint;

    [Header("Text")]
    [SerializeField] private string title = "Object";

    [TextArea(3, 10)]
    [SerializeField] private string description = "";

    [SerializeField] private TextStyle titleStyle = new TextStyle
    {
        fontSize = 30,
        color = new Color(1f, 0.84f, 0.42f),
        alignment = TextAnchor.UpperLeft
    };

    [SerializeField] private TextStyle descriptionStyle = new TextStyle
    {
        fontSize = 26,
        bold = false,
        alignment = TextAnchor.UpperLeft
    };

    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.8f);

    [Header("Camera Framing")]
    [Tooltip("How far the camera sits from the object.")]
    [SerializeField] private float viewDistance = 1.6f;

    [Tooltip("Turn around the object, in degrees.")]
    [SerializeField] private float viewYaw = 0f;

    [Tooltip("Look from above (positive) or below (negative), in degrees.")]
    [SerializeField] private float viewPitch = 12f;

    [Tooltip("Moves the point the camera aims at.")]
    [SerializeField] private Vector3 focusOffset = Vector3.zero;

    [SerializeField] private float transitionDuration = 0.45f;

    [Header("Camera Collision")]
    [Tooltip("Pull the inspection camera in when a wall is in the way.")]
    [SerializeField] private bool avoidCameraClipping = true;

    [SerializeField] private LayerMask cameraCollisionMask = ~0;

    [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.15f;

    [SerializeField, Min(0.01f)] private float minimumCameraDistance = 0.25f;

    [Header("Outline")]
    [Tooltip("Show a white outline while this object is the current interaction target.")]
    [SerializeField] private bool showOutline = true;

    [SerializeField] private Color outlineColor = Color.white;

    [Tooltip("Screen-space thickness. 0.004 is about 2 pixels at 1080p.")]
    [SerializeField, Range(0.0001f, 0.02f)] private float outlineWidth = 0.004f;

    [Tooltip("Optional root for the outline. Defaults to this object.")]
    [SerializeField] private Transform outlineRoot;

    [Header("Sounds")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip loopSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    // ---- panel geometry (the same for every object) ----
    private const float PanelAnchorY = 0.26f;
    private const float PanelPadding = 28f;
    private static readonly Vector2 PanelSize = new Vector2(1000f, 300f);

    // ---- shared UI, created once ----
    private static Canvas sharedCanvas;
    private static RectTransform sharedPanel;
    private static Image sharedBackground;
    private static Text sharedTitle;
    private static Text sharedBody;

    private Camera cam;
    private FirstPersonController controller;
    private AudioSource audioSource;
    private Coroutine transition;

    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private float savedNearClipPlane;
    private bool hasSavedCameraState;

    private bool inspecting;
    private int beginFrame = -1;

    public bool IsInspecting => inspecting;

    public KeyCode InteractionKey => interactKey;
    public string InteractionPrompt => prompt;
    public float InteractionRange => interactDistance;
    public int InteractionPriority => priority;
    public bool CanInteract =>
        !inspecting && transition == null && isActiveAndEnabled;

    public Transform InteractionPoint => interactionPoint != null
        ? interactionPoint
        : transform;

    public bool ShowOutline => showOutline;
    public Color OutlineColor => outlineColor;
    public float OutlineWidth => outlineWidth;
    public Transform OutlineRoot => outlineRoot != null
        ? outlineRoot
        : transform;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        InteractionRegistry.Register(this);
    }

    private void OnDisable()
    {
        InteractionRegistry.Unregister(this);

        if (inspecting || transition != null)
            AbortInspection();
    }

    private void OnDestroy()
    {
        if (inspecting || transition != null)
            AbortInspection();
    }

    /// <summary>
    /// Cleans up if the object is disabled or destroyed mid-inspection, so the
    /// player can never be left frozen with the panel stuck on screen.
    /// </summary>
    private void AbortInspection()
    {
        inspecting = false;

        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }

        if (cam != null)
        {
            cam.transform.localPosition = savedLocalPosition;
            cam.transform.localRotation = savedLocalRotation;

            if (hasSavedCameraState)
            {
                cam.nearClipPlane = savedNearClipPlane;
                hasSavedCameraState = false;
            }
        }

        if (loopSound != null && audioSource != null && audioSource.clip == loopSound)
            audioSource.Stop();

        HidePanel();
        UIState.PopModal(this);
        PlayerControl.Unlock(this);
    }

    private void Update()
    {
        if (!inspecting)
            return;

        // The same F press that started the inspection must not end it.
        if (Time.frameCount == beginFrame)
            return;

        if (Input.GetKeyDown(exitKey) || Input.GetKeyDown(KeyCode.Escape))
            EndInspect();
    }

    public void Interact()
    {
        if (!inspecting && transition == null)
            BeginInspect();
    }

    /// <summary>
    /// Closes the inspection view from code. The F/Esc input path uses the
    /// same routine, so cameras and locks are restored identically.
    /// </summary>
    public void StopInspecting()
    {
        if (inspecting)
            EndInspect();
    }

    private void BeginInspect()
    {
        controller = InteractionUtil.FindController();

        if (controller == null)
        {
            Debug.LogWarning(
                "InteractableObject: no player controller found.",
                this
            );
            return;
        }

        cam = controller.playerCamera != null
            ? controller.playerCamera
            : InteractionUtil.FindCamera();

        if (cam == null)
        {
            Debug.LogWarning(
                "InteractableObject: no player camera found.",
                this
            );
            return;
        }

        inspecting = true;
        beginFrame = Time.frameCount;

        UIState.PushModal(this);
        PlayerControl.Lock(this, controller);

        savedLocalPosition = cam.transform.localPosition;
        savedLocalRotation = cam.transform.localRotation;
        savedNearClipPlane = cam.nearClipPlane;
        hasSavedCameraState = true;

        // A tighter near plane keeps close-up framing from cutting through a
        // wall that the camera is right in front of.
        cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.05f);

        ShowPanel();
        Play(openSound);

        if (loopSound != null && audioSource != null)
        {
            audioSource.clip = loopSound;
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.Play();
        }

        StartTransition(TargetPosition(), TargetRotation(), false);
    }

    private void EndInspect()
    {
        if (!inspecting)
            return;

        inspecting = false;

        HidePanel();

        if (loopSound != null && audioSource != null && audioSource.clip == loopSound)
            audioSource.Stop();

        Play(closeSound);

        StartTransition(HomePosition(), HomeRotation(), true);
    }

    #region Camera

    private Vector3 HomePosition()
    {
        Transform parent = cam.transform.parent;

        return parent != null
            ? parent.TransformPoint(savedLocalPosition)
            : savedLocalPosition;
    }

    private Quaternion HomeRotation()
    {
        Transform parent = cam.transform.parent;

        return parent != null
            ? parent.rotation * savedLocalRotation
            : savedLocalRotation;
    }

    private Vector3 FocusPoint()
    {
        Collider col = GetComponentInChildren<Collider>();

        if (col != null)
            return col.bounds.center + focusOffset;

        Renderer mesh = GetComponentInChildren<Renderer>();

        if (mesh != null)
            return mesh.bounds.center + focusOffset;

        return transform.position + focusOffset;
    }

    private Vector3 TargetPosition()
    {
        float yaw = viewYaw * Mathf.Deg2Rad;
        float pitch = viewPitch * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(
            Mathf.Sin(yaw) * Mathf.Cos(pitch),
            Mathf.Sin(pitch),
            Mathf.Cos(yaw) * Mathf.Cos(pitch)
        );

        Vector3 focus = FocusPoint();
        float distance = Mathf.Max(minimumCameraDistance, viewDistance);

        if (avoidCameraClipping)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                focus,
                cameraCollisionRadius,
                direction,
                distance,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore
            );

            float closest = distance;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;

                if (hitCollider == null)
                    continue;

                // The object being inspected and the player holding the
                // camera must not push the camera around.
                if (hitCollider.transform == transform ||
                    hitCollider.transform.IsChildOf(transform))
                    continue;

                if (controller != null &&
                    hitCollider.transform.IsChildOf(controller.transform))
                    continue;

                if (hits[i].distance < closest)
                    closest = hits[i].distance;
            }

            if (closest < distance)
            {
                distance = Mathf.Max(
                    minimumCameraDistance,
                    closest - cameraCollisionRadius
                );
            }
        }

        return focus + direction * distance;
    }

    private Quaternion TargetRotation()
    {
        return Quaternion.LookRotation(
            FocusPoint() - TargetPosition(),
            Vector3.up
        );
    }

    private void StartTransition(
        Vector3 worldPosition,
        Quaternion worldRotation,
        bool restoreOnFinish)
    {
        if (transition != null)
            StopCoroutine(transition);

        transition = StartCoroutine(
            MoveCamera(worldPosition, worldRotation, restoreOnFinish)
        );
    }

    private IEnumerator MoveCamera(
        Vector3 targetPosition,
        Quaternion targetRotation,
        bool restoreOnFinish)
    {
        Vector3 startPosition = cam.transform.position;
        Quaternion startRotation = cam.transform.rotation;

        float duration = Mathf.Max(0.0001f, transitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            cam.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            cam.transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        cam.transform.position = targetPosition;
        cam.transform.rotation = targetRotation;
        transition = null;

        if (!restoreOnFinish)
            yield break;

        // Put the camera back where the player controller expects it.
        cam.transform.localPosition = savedLocalPosition;
        cam.transform.localRotation = savedLocalRotation;

        if (hasSavedCameraState)
        {
            cam.nearClipPlane = savedNearClipPlane;
            hasSavedCameraState = false;
        }

        UIState.PopModal(this);
        PlayerControl.Unlock(this);
    }

    #endregion

    private void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    #region Panel

    private void ShowPanel()
    {
        EnsurePanel();

        sharedBackground.color = panelColor;

        titleStyle.Apply(sharedTitle);
        descriptionStyle.Apply(sharedBody);

        bool hasTitle = !string.IsNullOrEmpty(title);

        sharedTitle.gameObject.SetActive(hasTitle);
        sharedTitle.text = hasTitle ? title : string.Empty;
        sharedBody.text = description;

        RectTransform bodyRect = sharedBody.rectTransform;

        bodyRect.anchoredPosition = hasTitle
            ? new Vector2(PanelPadding, -(PanelPadding + 46f))
            : new Vector2(PanelPadding, -PanelPadding);

        bodyRect.sizeDelta = new Vector2(
            PanelSize.x - PanelPadding * 2f,
            PanelSize.y - PanelPadding * 2f - (hasTitle ? 46f : 0f)
        );

        sharedPanel.gameObject.SetActive(true);
    }

    private void HidePanel()
    {
        if (sharedPanel != null)
            sharedPanel.gameObject.SetActive(false);
    }

    private static void EnsurePanel()
    {
        if (sharedCanvas != null)
            return;

        sharedCanvas = UIHelper.CreateCanvas("InteractableCanvas", 250, true);

        sharedPanel = UIHelper.CreateRect(
            "InteractPanel",
            sharedCanvas.transform,
            new Vector2(0.5f, PanelAnchorY),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            PanelSize
        );

        sharedBackground = UIHelper.AddPanel(sharedPanel, Color.black);
        UIHelper.AddMask(sharedPanel);

        RectTransform titleRect = UIHelper.CreateRect(
            "Title",
            sharedPanel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(PanelPadding, -PanelPadding),
            new Vector2(PanelSize.x - PanelPadding * 2f, 40f)
        );

        sharedTitle = UIHelper.AddText(titleRect, new TextStyle(), string.Empty);

        RectTransform bodyRect = UIHelper.CreateRect(
            "Body",
            sharedPanel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(PanelPadding, -(PanelPadding + 46f)),
            new Vector2(
                PanelSize.x - PanelPadding * 2f,
                PanelSize.y - PanelPadding * 2f - 46f
            )
        );

        sharedBody = UIHelper.AddText(bodyRect, new TextStyle(), string.Empty);

        sharedPanel.gameObject.SetActive(false);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactDistance);

        Gizmos.color = Color.yellow;

        Vector3 focus = FocusPoint();
        Gizmos.DrawLine(focus, TargetPosition());
        Gizmos.DrawWireSphere(TargetPosition(), 0.08f);
    }
}
