using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// The cashier. Press E to check out.
///
/// Shopping complete  -> long conversation -> transformation -> success event.
/// Shopping incomplete -> short conversation -> failure effect -> list reopens.
///
/// Every asset, animation trigger, sound and duration is optional; missing
/// optional references are skipped instead of throwing.
/// </summary>
public class CashierInteraction : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float range = 3.5f;
    [SerializeField] private string prompt = "E - Talk to cashier";
    [SerializeField] private int priority;
    [SerializeField] private Transform interactionPoint;

    [Header("Successful Checkout Dialogue")]
    [SerializeField] private DialogueSource successDialogue = new DialogueSource
    {
        conversationId = "CASHIER_Success_01",
        lines = new DialogueLine[]
        {
            new DialogueLine { speaker = "Cashier", text = "Evening. Did you find everything you were looking for?" },
            new DialogueLine { speaker = "You", text = "Yeah. I got everything on the list." },
            new DialogueLine { speaker = "Cashier", text = "Good. Let's get you checked out." },
            new DialogueLine { speaker = "Cashier", text = "You live around here?", delayBefore = 1.5f },
            new DialogueLine { speaker = "You", text = "Yeah. A few streets away." },
            new DialogueLine { speaker = "Cashier", text = "Must be quiet at night." },
            new DialogueLine { speaker = "You", text = "Usually." },
            new DialogueLine { speaker = "Cashier", text = "Not tonight?", speakerColor = new Color(0.85f, 0.05f, 0.05f) },
            new DialogueLine { speaker = "You", text = "What?" },
            new DialogueLine { speaker = "Cashier", text = "Nothing. Just making conversation." },
            new DialogueLine { speaker = "Cashier", text = "You should get home soon.", delayBefore = 0.8f, speakerColor = new Color(0.85f, 0.05f, 0.05f) },
            new DialogueLine { speaker = "You", text = "Why? Is something happening?" },
            new DialogueLine { speaker = "Cashier", text = "Stores close. Streets get empty. People get lost.", speakerColor = new Color(0.85f, 0.05f, 0.05f) },
            new DialogueLine { speaker = "You", text = "That's... a weird thing to say." },
            new DialogueLine { speaker = "Cashier", text = "Have a pleasant night.", delayBefore = 1f, speakerColor = new Color(0.85f, 0.05f, 0.05f) }
        }
    };

    [Tooltip("Zero-based line after which the scanner sounds play. Line 3 = 2.")]
    [SerializeField] private int scannerAfterLineIndex = 2;

    [Header("Incomplete Checkout Dialogue")]
    [SerializeField] private DialogueSource incompleteDialogue = new DialogueSource
    {
        conversationId = "CASHIER_Incomplete_01",
        lines = new DialogueLine[]
        {
            new DialogueLine { speaker = "Cashier", text = "Hold on. I think you're missing something." },
            new DialogueLine { speaker = "You", text = "What? I got everything." },
            new DialogueLine { speaker = "Cashier", text = "Not everything on your list." },
            new DialogueLine { speaker = "You", text = "Are you sure?" },
            new DialogueLine { speaker = "Cashier", text = "Would you like to check again?" }
        }
    };

    [Header("Objective")]
    [SerializeField] private string payObjectiveId = "pay_groceries";

    [Header("Scanner Sounds")]
    [SerializeField] private AudioSource scannerSource;
    [SerializeField] private AudioClip[] scannerClips;
    [SerializeField] private float scannerSequenceDuration = 1.5f;

    [Header("Transformation - Models")]
    [Tooltip("The ordinary cashier. Restored after the sequence if enabled.")]
    [SerializeField] private GameObject normalModel;

    [Tooltip("The transformed model. Optional.")]
    [SerializeField] private GameObject transformedModel;

    [SerializeField] private Animator animator;
    [SerializeField] private string idleTrigger = "";
    [SerializeField] private string transformationTrigger = "";
    [SerializeField] private string restoreTrigger = "";

    [Header("Transformation - Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip transformationSound;
    [SerializeField] private AudioClip failureSound;

    [Tooltip("Store ambience that fades out during the sequence.")]
    [SerializeField] private AudioSource ambienceSource;

    [Header("Transformation - Timing")]
    [SerializeField] private float preTransformationDelay = 1.5f;
    [SerializeField] private float silencePhase = 0.5f;
    [SerializeField] private float visualPausePhase = 0.75f;
    [SerializeField] private float transformationPhase = 2f;

    [Tooltip("Development default. Turn off for a permanent ending later.")]
    [SerializeField] private bool restoreAfterTransformation = true;

    [Header("Transformation - Speaker Colour")]
    [SerializeField] private string transformedSpeaker = "Cashier";
    [SerializeField] private Color transformedSpeakerColor =
        new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private Color normalSpeakerColor =
        new Color(0.35f, 1f, 0.45f);

    [Tooltip(
        "Zero-based line where the cashier starts sounding wrong. Cashier " +
        "lines from here use the transformed colour when they have no " +
        "per-line colour of their own."
    )]
    [SerializeField] private int creepyLineStartIndex = 7;

    [Header("Failure Path")]
    [SerializeField] private bool showListAfterIncomplete = true;
    [SerializeField] private bool playFailureEffect = true;
    [SerializeField] private float failureEffectDelay = 0.75f;
    [SerializeField] private string failureTrigger = "";

    [Header("Ending")]
    [Tooltip(
        "Load the end-of-demo scene after the successful checkout finishes. " +
        "This is a code default, so Store.unity needs no changes.")]
    [SerializeField] private bool goToEndScene = true;

    [Tooltip("Scene loaded when the demo ends.")]
    [SerializeField] private string endSceneName = "End";

    [Min(0f)]
    [Tooltip("Pause after the cashier returns to normal before the ending.")]
    [SerializeField] private float endSceneDelay = 2.5f;

    [Header("Sequence - Camera")]
    [Tooltip("Shared camera lock / move controller used during checkout.")]
    [SerializeField] private CameraFocusController cameraFocus;
    [SerializeField] private Transform cameraDialogueAnchor;
    [SerializeField] private Transform cameraTransformAnchor;
    [SerializeField] private Transform cameraApproachAnchor;
    [SerializeField] private Transform cameraApproachLookTarget;
    [SerializeField] private Transform cameraReachAnchor;
    [SerializeField] private Transform cashierLookTarget;

    [SerializeField, Min(0.05f)] private float cameraDialogueDuration = 1.4f;
    [SerializeField, Min(0.05f)] private float cameraTransformDuration = 2.4f;
    [SerializeField, Min(0.05f)] private float cameraApproachDuration = 2f;
    [SerializeField, Min(0.05f)] private float cameraReachDuration = 1.5f;
    [SerializeField, Min(0.05f)] private float cameraReturnDuration = 1.4f;

    [Header("Sequence - Lighting")]
    [SerializeField] private StoreLightingController lighting;
    [SerializeField, Min(0.05f)] private float lightingTransitionDuration = 3f;
    [SerializeField, Min(0f)] private float lightingFlickerDuration = 1.4f;
    [SerializeField, Min(0.05f)] private float restoreLightingDuration = 1.2f;

    [Header("Sequence - Actors")]
    [SerializeField] private CashierAnimationDriver cashierAnimation;
    [SerializeField] private MonsterAnimationDriver monsterAnimation;
    [SerializeField] private MonsterApproachController monsterApproach;

    [Header("Sequence - Monster Reveal")]
    [Tooltip("Beat between the cashier vanishing and the pumpkin appearing.")]
    [SerializeField, Min(0f)] private float monsterRevealDelay = 0.45f;
    [SerializeField, Min(1f)] private float monsterApproachTimeout = 20f;
    [SerializeField, Min(0.5f)] private float reachDuration = 2.4f;
    [SerializeField, Min(0f)] private float reachHoldDuration = 1.8f;

    [Header("Events")]
    [SerializeField] private UnityEvent onCheckoutStarted;
    [SerializeField] private UnityEvent onTransformationStart;
    [SerializeField] private UnityEvent onTransformationComplete;
    [SerializeField] private UnityEvent onCheckoutSuccess;
    [SerializeField] private UnityEvent onCheckoutFailed;

    private bool busy;
    private bool checkoutCompleted;
    private bool cashierLockFallback;
    private bool sequenceAborted;
    private DialogueLine[] activeSuccessLines;
    private float savedAmbienceVolume = -1f;

    public KeyCode InteractionKey => interactionKey;
    public string InteractionPrompt => prompt;
    public float InteractionRange => range;
    public int InteractionPriority => priority;
    public bool CanInteract =>
        !busy && !checkoutCompleted && isActiveAndEnabled;

    public bool CheckoutCompleted => checkoutCompleted;

    /// <summary>
    /// Testing helper: turn this off to stay in the store after checkout
    /// instead of loading the ending.
    /// </summary>
    public bool EndsDemo
    {
        get => goToEndScene;
        set => goToEndScene = value;
    }

    public Transform InteractionPoint => interactionPoint != null
        ? interactionPoint
        : transform;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        if (cameraFocus == null)
            cameraFocus = FindAnyObjectByType<CameraFocusController>();

        if (lighting == null)
            lighting = FindAnyObjectByType<StoreLightingController>();

        if (monsterApproach == null)
            monsterApproach = FindAnyObjectByType<MonsterApproachController>();

        if (cashierAnimation == null && normalModel != null)
        {
            cashierAnimation =
                normalModel.GetComponent<CashierAnimationDriver>();

            if (cashierAnimation == null)
                cashierAnimation =
                    normalModel.GetComponentInChildren<CashierAnimationDriver>(true);
        }

        if (monsterAnimation == null && transformedModel != null)
        {
            monsterAnimation =
                transformedModel.GetComponent<MonsterAnimationDriver>();

            if (monsterAnimation == null)
                monsterAnimation =
                    transformedModel.GetComponentInChildren<MonsterAnimationDriver>(true);
        }

        if (normalModel == gameObject || transformedModel == gameObject)
        {
            Debug.LogWarning(
                "CashierInteraction: assign child model objects, not the object " +
                "holding this component, otherwise the sequence would disable " +
                "itself.",
                this
            );
        }
    }

    private void OnEnable()
    {
        InteractionRegistry.Register(this);
    }

    private void OnDisable()
    {
        InteractionRegistry.Unregister(this);

        if (busy || (cameraFocus != null && cameraFocus.IsActive))
            AbortSequence();
    }

    private void OnDestroy()
    {
        if (busy || (cameraFocus != null && cameraFocus.IsActive))
            AbortSequence();
        else
        {
            UIState.PopModal(this);
            PlayerControl.Unlock(this);
        }
    }

    public void Interact()
    {
        if (busy || checkoutCompleted)
            return;

        StartCoroutine(CheckoutRoutine());
    }

    private IEnumerator CheckoutRoutine()
    {
        busy = true;
        onCheckoutStarted?.Invoke();

        ShoppingCart cart = ShoppingCart.Instance;

        if (cart == null || !cart.IsComplete)
        {
            yield return PlayRoutine(incompleteDialogue);

            if (playFailureEffect)
                PlayFailureEffect();

            onCheckoutFailed?.Invoke();

            if (failureEffectDelay > 0f)
                yield return new WaitForSecondsRealtime(failureEffectDelay);

            if (showListAfterIncomplete)
                QuestLogUI.Ensure().Open();

            busy = false;
            yield break;
        }

        // Optional checkout cue. A missing clip keeps the store theme going.
        ToastUI.HideCurrent();
        MusicManager.PlayCue(MusicCueIds.StoreCheckout);

        bool done = false;
        DialogueSystem system = DialogueSystem.Ensure();

        ApplyCreepyLineColors(successDialogue);
        activeSuccessLines = successDialogue.ResolveLines();
        sequenceAborted = false;

        bool cameraFocused = BeginSequenceCamera();

        if (!cameraFocused)
        {
            cashierLockFallback = true;
            PlayerControl.Lock(this);
        }

        system.Play(
            successDialogue,
            () => done = true,
            HandleLineFinished,
            HandleLineStarted
        );

        while (!done && system != null && !sequenceAborted)
            yield return null;

        if (sequenceAborted)
        {
            busy = false;
            yield break;
        }

        UIState.PushModal(this);

        if (preTransformationDelay > 0f)
            yield return new WaitForSecondsRealtime(preTransformationDelay);

        yield return TransformationRoutine();

        if (sequenceAborted)
        {
            busy = false;
            yield break;
        }

        checkoutCompleted = true;

        ObjectiveManager.Ensure().CompleteObjective(payObjectiveId);

        onCheckoutSuccess?.Invoke();

        if (goToEndScene)
        {
            if (endSceneDelay > 0f)
                yield return new WaitForSecondsRealtime(endSceneDelay);

            GoToEndScene();
        }

        busy = false;
    }

    /// <summary>
    /// Leaves the demo for the ending scene. SceneFlow is preferred because it
    /// owns the loading screen and cleans up the story session; a direct load
    /// is the fallback when the flow is already busy.
    /// </summary>
    private void GoToEndScene()
    {
        string scene = string.IsNullOrEmpty(endSceneName)
            ? "End"
            : endSceneName;

        if (SceneFlow.Go(new TransitionRequest
        {
            sceneName = scene,
            chapter = null,
            showChapterIntro = false,
            reason = TransitionReason.Direct,
            saveProgress = false
        }))
        {
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(scene))
        {
            SceneManager.LoadScene(scene);
            return;
        }

        Debug.LogWarning(
            "CashierInteraction: ending scene '" + scene +
            "' is not in Build Settings, so the demo stays in the store.",
            this
        );
    }

    /// <summary>
    /// Existing scenes may have serialized this dialogue before per-line
    /// colours existed. Paint the cashier's later lines red at runtime so the
    /// scary shift is always visible.
    /// </summary>
    private void ApplyCreepyLineColors(DialogueSource source)
    {
        if (source == null)
            return;

        DialogueLine[] lines = source.ResolveLines();

        if (lines == null)
            return;

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine line = lines[i];

            if (line == null || line.speakerColor.a > 0f)
                continue;

            if (i < creepyLineStartIndex)
                continue;

            if (!string.Equals(
                    line.speaker,
                    transformedSpeaker,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            line.speakerColor = transformedSpeakerColor;
        }
    }

    private IEnumerator PlayRoutine(DialogueSource source)
    {
        if (source == null || !source.HasContent)
            yield break;

        DialogueSystem system = DialogueSystem.Ensure();
        bool done = false;

        system.Play(source, () => done = true);

        while (!done && system != null)
            yield return null;
    }

    private void HandleLineFinished(int index)
    {
        if (index == scannerAfterLineIndex)
            PlayScannerSequence();
    }

    private void HandleLineStarted(int index)
    {
        DialogueLine line = null;

        if (activeSuccessLines != null &&
            index >= 0 &&
            index < activeSuccessLines.Length)
        {
            line = activeSuccessLines[index];
        }

        if (cashierAnimation != null)
        {
            cashierAnimation.PlayLineGesture(
                index,
                line != null ? line.speaker : null
            );
        }

        if (index < creepyLineStartIndex)
            return;

        if (cashierAnimation != null)
            cashierAnimation.SetCreepy(true);

        // A single short flicker as the first wrong line lands. The full red
        // wash starts later, when the model actually changes.
        if (index == creepyLineStartIndex && lighting != null)
            lighting.Flicker(0.35f, 0.5f);
    }

    private bool BeginSequenceCamera()
    {
        if (cameraFocus == null ||
            cameraDialogueAnchor == null ||
            cashierLookTarget == null)
        {
            return false;
        }

        return cameraFocus.Focus(
            cameraDialogueAnchor,
            cashierLookTarget,
            cameraDialogueDuration
        );
    }

    private void AbortSequence()
    {
        sequenceAborted = true;

        if (cameraFocus != null)
            cameraFocus.Abort();

        if (lighting != null)
            lighting.RestoreImmediate();

        if (monsterApproach != null)
            monsterApproach.RestoreOriginalTransform();

        if (monsterAnimation != null)
            monsterAnimation.ResetPose();

        if (cashierAnimation != null)
        {
            cashierAnimation.SetCreepy(false);
            cashierAnimation.ResetPose();
        }

        if (transformedModel != null &&
            transformedModel != normalModel &&
            transformedModel != gameObject)
        {
            transformedModel.SetActive(false);
        }

        if (normalModel != null && normalModel != gameObject)
            normalModel.SetActive(true);

        if (ambienceSource != null)
        {
            if (savedAmbienceVolume >= 0f)
                ambienceSource.volume = savedAmbienceVolume;

            savedAmbienceVolume = -1f;
        }

        UIState.PopModal(this);
        PlayerControl.Unlock(this);

        cashierLockFallback = false;
        busy = false;
    }

    #region Scanner and failure

    public void PlayScannerSequence()
    {
        if (scannerClips == null || scannerClips.Length == 0)
            return;

        StartCoroutine(ScannerRoutine());
    }

    private IEnumerator ScannerRoutine()
    {
        AudioSource source = scannerSource != null
            ? scannerSource
            : audioSource;

        int count = scannerClips.Length;
        float slot = Mathf.Max(0.1f, scannerSequenceDuration) / count;

        for (int i = 0; i < count; i++)
        {
            if (scannerClips[i] != null && source != null)
                source.PlayOneShot(scannerClips[i]);

            yield return new WaitForSecondsRealtime(slot);
        }
    }

    private void PlayFailureEffect()
    {
        if (failureSound != null)
        {
            AudioSource source = scannerSource != null
                ? scannerSource
                : audioSource;

            if (source != null)
                source.PlayOneShot(failureSound);
        }

        if (animator != null && !string.IsNullOrEmpty(failureTrigger))
            SetAnimatorTrigger(failureTrigger);
    }

    /// <summary>
    /// Optional animator triggers are only safe to fire when the model
    /// actually has a controller. The authored characters are animated
    /// procedurally, so calling SetTrigger on an empty Animator would only
    /// produce a console warning every checkout.
    /// </summary>
    private void SetAnimatorTrigger(string trigger)
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null ||
            string.IsNullOrEmpty(trigger))
        {
            return;
        }

        animator.SetTrigger(trigger);
    }

    #endregion

    #region Transformation

    private IEnumerator TransformationRoutine()
    {
        // Phase 1 - silence. Any onboarding toast still on screen would break
        // the cinematic, so clear it here as well as at checkout start.
        ToastUI.HideCurrent();
        savedAmbienceVolume = ambienceSource != null
            ? ambienceSource.volume
            : -1f;

        if (ambienceSource != null)
        {
            StartCoroutine(
                FadeVolume(
                    ambienceSource,
                    savedAmbienceVolume,
                    0f,
                    silencePhase
                )
            );
        }

        if (silencePhase > 0f)
            yield return new WaitForSecondsRealtime(silencePhase);

        // Phase 2 - visual pause. The cashier starts to lose control while the
        // store lights stutter, but everything is still normal-coloured.
        if (cashierAnimation != null)
            cashierAnimation.PlayUnease();

        if (lighting != null)
            lighting.Flicker(0.45f, 0.55f);

        if (animator != null && !string.IsNullOrEmpty(idleTrigger))
            SetAnimatorTrigger(idleTrigger);

        if (visualPausePhase > 0f)
            yield return new WaitForSecondsRealtime(visualPausePhase);

        // Phase 3 - the reveal. Lighting, camera, sound and the model swap all
        // start from the same beat.
        MusicManager.PlayCue(MusicCueIds.ScaryCheckout);
        onTransformationStart?.Invoke();

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.SetSpeakerColor(
                transformedSpeaker,
                transformedSpeakerColor
            );
        }

        if (lighting != null)
        {
            lighting.BeginRed(
                lightingTransitionDuration,
                lightingFlickerDuration
            );
        }

        if (cameraFocus != null &&
            cameraTransformAnchor != null &&
            cashierLookTarget != null)
        {
            cameraFocus.MoveTo(
                cameraTransformAnchor,
                cashierLookTarget,
                cameraTransformDuration
            );
        }

        if (cashierAnimation != null)
            cashierAnimation.PlayTransformation();

        if (transformationSound != null && audioSource != null)
            audioSource.PlayOneShot(transformationSound);

        if (monsterRevealDelay > 0f)
            yield return new WaitForSecondsRealtime(monsterRevealDelay);

        if (normalModel != null &&
            normalModel != transformedModel &&
            normalModel != gameObject)
        {
            normalModel.SetActive(false);
        }

        if (transformedModel != null && transformedModel != gameObject)
            transformedModel.SetActive(true);

        if (monsterApproach != null && cameraFocus != null &&
            cameraFocus.Camera != null)
        {
            monsterApproach.SetFaceTarget(cameraFocus.Camera.transform);
        }

        if (monsterAnimation != null)
            monsterAnimation.Prepare();

        if (animator != null && !string.IsNullOrEmpty(transformationTrigger))
            SetAnimatorTrigger(transformationTrigger);

        if (transformationPhase > 0f)
            yield return new WaitForSecondsRealtime(transformationPhase);

        if (sequenceAborted)
            yield break;

        // Phase 4 - the approach. Pull the camera back first so the whole
        // monster is visible when it rounds the counter, then let the
        // controller walk its authored path.
        if (cameraFocus != null && cameraApproachAnchor != null)
        {
            Transform approachLook = cameraApproachLookTarget != null
                ? cameraApproachLookTarget
                : cashierLookTarget;

            if (approachLook != null)
            {
                cameraFocus.MoveTo(
                    cameraApproachAnchor,
                    approachLook,
                    cameraApproachDuration
                );
            }
        }

        if (monsterApproach != null)
        {
            Coroutine approach = StartCoroutine(
                monsterApproach.ApproachRoutine()
            );

            float startedAt = Time.unscaledTime;

            while (monsterApproach.IsApproaching &&
                   Time.unscaledTime - startedAt < monsterApproachTimeout &&
                   !sequenceAborted)
            {
                yield return null;
            }

            if (monsterApproach.IsApproaching)
            {
                StopCoroutine(approach);

                if (!sequenceAborted)
                {
                    Debug.LogWarning(
                        "CashierInteraction: monster approach timed out; " +
                        "continuing with the reach.",
                        this
                    );
                }
            }
        }

        if (sequenceAborted)
            yield break;

        // Phase 5 - reach. The camera moves to the close shot while the
        // monster raises both hands toward it.
        if (cameraFocus != null && cameraReachAnchor != null)
        {
            Vector3 lookAt = monsterApproach != null
                ? monsterApproach.FocusPoint
                : cameraReachAnchor.position + cameraReachAnchor.forward;

            cameraFocus.MoveTo(
                cameraReachAnchor.position,
                lookAt,
                cameraReachDuration
            );
        }

        Transform reachTarget = cameraFocus != null &&
            cameraFocus.Camera != null
            ? cameraFocus.Camera.transform
            : null;

        if (monsterAnimation != null && reachTarget != null)
            monsterAnimation.BeginReach(reachTarget, reachDuration);

        if (reachDuration > 0f)
            yield return new WaitForSecondsRealtime(reachDuration);

        if (reachHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(reachHoldDuration);

        onTransformationComplete?.Invoke();

        // Phase 6 - the grab ends with a hard cut. Lower the arms while the
        // camera snaps straight back to the player, so it feels like being
        // thrown back into your own body instead of a slow cinematic dolly.
        if (monsterAnimation != null)
            monsterAnimation.ReleaseReach(cameraReturnDuration);

        if (cameraFocus != null && cameraFocus.IsActive)
            cameraFocus.Abort();

        if (sequenceAborted)
            yield break;

        if (cashierLockFallback)
        {
            PlayerControl.Unlock(this);
            cashierLockFallback = false;
        }

        UIState.PopModal(this);

        // The ending scene replaces the store, so the final red tableau is
        // allowed to persist into the transition. When the demo stays in the
        // store, everything is restored exactly as it was.
        bool restoreVisuals = restoreAfterTransformation && !goToEndScene;

        if (restoreVisuals)
        {
            if (lighting != null)
                lighting.Restore(restoreLightingDuration);

            if (monsterApproach != null)
                monsterApproach.RestoreOriginalTransform();

            if (monsterAnimation != null)
                monsterAnimation.ResetPose();

            if (transformedModel != null &&
                transformedModel != normalModel &&
                transformedModel != gameObject)
            {
                transformedModel.SetActive(false);
            }

            if (normalModel != null && normalModel != gameObject)
                normalModel.SetActive(true);

            if (animator != null && !string.IsNullOrEmpty(restoreTrigger))
                SetAnimatorTrigger(restoreTrigger);

            if (cashierAnimation != null)
            {
                cashierAnimation.SetCreepy(false);
                cashierAnimation.ResetPose();
            }

            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.SetSpeakerColor(
                    transformedSpeaker,
                    normalSpeakerColor
                );
            }
        }

        if (ambienceSource != null && savedAmbienceVolume >= 0f)
            ambienceSource.volume = savedAmbienceVolume;

        savedAmbienceVolume = -1f;
    }

    private IEnumerator FadeVolume(
        AudioSource source,
        float from,
        float to,
        float duration)
    {
        if (source == null)
            yield break;

        if (duration <= 0f)
        {
            source.volume = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (source == null)
                yield break;

            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        if (source != null)
            source.volume = to;
    }

    #endregion

    /// <summary>Testing / replay helper.</summary>
    public void ResetCheckout()
    {
        busy = false;
        checkoutCompleted = false;
        sequenceAborted = false;
        activeSuccessLines = null;

        if (cameraFocus != null)
            cameraFocus.Abort();

        if (lighting != null)
            lighting.RestoreImmediate();

        if (monsterApproach != null)
            monsterApproach.RestoreOriginalTransform();

        if (monsterAnimation != null)
            monsterAnimation.ResetPose();

        if (cashierAnimation != null)
        {
            cashierAnimation.SetCreepy(false);
            cashierAnimation.ResetPose();
        }

        if (ambienceSource != null && savedAmbienceVolume >= 0f)
            ambienceSource.volume = savedAmbienceVolume;

        savedAmbienceVolume = -1f;

        if (normalModel != null && normalModel != gameObject)
            normalModel.SetActive(true);

        if (transformedModel != null &&
            transformedModel != normalModel &&
            transformedModel != gameObject)
        {
            transformedModel.SetActive(false);
        }

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.SetSpeakerColor(
                transformedSpeaker,
                normalSpeakerColor
            );
        }

        UIState.PopModal(this);
        PlayerControl.Unlock(this);

        // Replaying the checkout should return to the current act theme.
        if (Application.isPlaying)
        {
            MusicManager.PlaySceneMusic(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
