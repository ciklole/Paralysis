using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central story sequencer. Owns the order of the opening, the lights
/// objective, the store arrival, the shopping beats and the checkout
/// objective. Scene names, dialogue and timings are all Inspector fields.
///
/// Put one on the persistent managers object. Duplicates delete themselves.
/// </summary>
public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance;

    [Header("Scene Roles")]
    [Tooltip("Scene name exactly as it appears in Build Settings.")]
    [SerializeField] private string apartmentScene = "Home";

    [SerializeField] private string cityScene = "City";

    [Tooltip("Must be added to Build Settings. Leave empty until it exists.")]
    [SerializeField] private string storeScene = "";

    [Header("Apartment Opening")]
    [SerializeField] private DialogueSource apartmentOpening = new DialogueSource
    {
        conversationId = "APT_Opening_01",
        lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = "You",
                text = "Alright. Let's get this over with."
            },
            new DialogueLine
            {
                speaker = "You",
                text = "I should probably get some groceries before it gets too late."
            },
            new DialogueLine
            {
                speaker = "You",
                text = "Where did I put that shopping list?"
            },
            new DialogueLine
            {
                speaker = "You",
                text = "Ah, there it is. Bread, milk, eggs... I think that's everything."
            }
        }
    };

    [Tooltip("Delay after the player is ready.")]
    [SerializeField] private float apartmentIntroDelay = 1f;

    [Tooltip("Wait after the conversation before objective 1 appears.")]
    [SerializeField] private float objectiveActivationDelay = 0.5f;

    [SerializeField] private bool playApartmentOpening = true;

    [Header("Objective 1 - Lights")]
    [SerializeField] private LightsObjective lightsObjective;

    [SerializeField] private DialogueSource lightsCompletedDialogue =
        new DialogueSource
        {
            conversationId = "APT_LightsDone_01",
            lines = new DialogueLine[]
            {
                new DialogueLine
                {
                    speaker = "You",
                    text = "Finally. Let's go."
                }
            }
        };

    [Tooltip("Wait after the completion notification before the optional line.")]
    [SerializeField] private float lightsNotificationDelay = 0.5f;

    [SerializeField] private bool playLightsCompletedDialogue = true;

    [Header("Store Arrival")]
    [SerializeField] private DialogueSource storeOpening = new DialogueSource
    {
        conversationId = "STORE_Opening_01",
        lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = "You",
                text = "Alright. Let's see what we need."
            },
            new DialogueLine
            {
                speaker = "You",
                text = "Bread, milk, eggs... should be easy enough."
            }
        }
    };

    [Tooltip("Delay after the store scene is ready.")]
    [SerializeField] private float storeIntroDelay = 0.75f;

    [SerializeField]
    private string storeInstruction =
        "Press M to view your shopping list. Find each item and press F to collect it.";

    [SerializeField] private float storeInstructionDuration = 5f;

    [SerializeField] private bool autoOpenQuestLogInStore = true;

    [Tooltip("Seconds after the instruction before the log opens. 0 = immediately.")]
    [SerializeField] private float autoOpenQuestLogDelay = 5f;

    [Tooltip("Only complete the drive objective when the car started the trip.")]
    [SerializeField] private bool requireCarTripForStoreCompletion = true;

    [Header("Shopping")]
    [SerializeField] private ShoppingCart cart;

    [SerializeField] private DialogueSource firstPickupDialogue = new DialogueSource
    {
        conversationId = "STORE_FirstPickup_01",
        lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = "You",
                text = "One down."
            }
        }
    };

    [SerializeField] private DialogueSource allCollectedDialogue = new DialogueSource
    {
        conversationId = "STORE_AllCollected_01",
        lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = "You",
                text = "That's everything. Time to pay."
            }
        }
    };

    [SerializeField] private bool playFirstPickupDialogue = true;
    [SerializeField] private bool playAllCollectedDialogue = true;

    [Header("Objective 1 - Ids and Text")]
    [SerializeField] private string lightsObjectiveId = "turn_off_lights";
    [SerializeField] private string lightsObjectiveText = "Turn off all the lights in the house";
    [SerializeField] private string goToCarObjectiveId = "go_to_car";
    [SerializeField] private string goToCarObjectiveText = "Go outside to the car";

    [Header("Objectives 3 to 5 - Ids and Text")]
    [SerializeField] private string driveObjectiveId = "drive_to_store";
    [SerializeField] private string driveObjectiveText = "Drive to the grocery store";
    [SerializeField] private string collectObjectiveId = "collect_groceries";
    [SerializeField] private string collectObjectiveText = "Collect the required groceries";
    [SerializeField] private string payObjectiveId = "pay_groceries";
    [SerializeField] private string payObjectiveText = "Pay for the groceries";

    [Header("Debug")]
    [Tooltip("F8 = reset session, F9 = skip to store, F10 = fill cart.")]
    [SerializeField] private bool enableDebugKeys = true;

    private bool apartmentIntroHandled;
    private bool lightsSequenceRunning;
    private bool lightsCompletedHandled;
    private bool storeIntroHandled;
    private bool firstPickupHandled;
    private bool allCollectedHandled;
    private bool pendingStoreTrip;
    private bool cartSubscribed;
    private bool handledSceneLoad;

    public string StoreScene => storeScene;
    public string CityScene => cityScene;
    public bool PendingStoreTrip => pendingStoreTrip;

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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (cart != null && cartSubscribed)
        {
            cart.Changed -= OnCartChanged;
            cart.Completed -= OnCartCompleted;
        }

        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        PlayerInteractor.Ensure();

        ValidateScenes();
        ResolveReferences();

        // A scene that was loaded after this component awoke already ran its
        // routine from the sceneLoaded callback. Calling it again from Start
        // would stop that routine after it claimed its one-shot flag, leaving
        // the apartment intro and its objective permanently skipped.
        if (handledSceneLoad)
        {
            handledSceneLoad = false;
            return;
        }

        HandleScene(SceneManager.GetActiveScene().name);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        handledSceneLoad = true;
        HandleScene(scene.name);
    }

    private void HandleScene(string sceneName)
    {
        // Hand-placed HUD text and TextMesh signs should match the runtime UI.
        GameFont.ApplyToAll();

        StopAllCoroutines();

        // Nothing from the previous scene may keep owning input.
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.StopImmediately();

        ToastUI.HideCurrent();

        StartCoroutine(SceneRoutine(sceneName));
    }

    private void ResolveReferences()
    {
        if (cart == null)
            cart = ShoppingCart.Instance;

        if (cart != null && !cartSubscribed)
        {
            cart.Changed += OnCartChanged;
            cart.Completed += OnCartCompleted;
            cartSubscribed = true;
        }
    }

    private void ValidateScenes()
    {
        ValidateScene("apartment", apartmentScene);
        ValidateScene("city", cityScene);
        ValidateScene("store", storeScene);
    }

    private void ValidateScene(string role, string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        if (Application.CanStreamedLevelBeLoaded(sceneName))
            return;

        Debug.LogError(
            "StoryDirector: the " + role + " scene '" + sceneName +
            "' is not in Build Settings. Add it or update the scene role.",
            this
        );
    }

    private IEnumerator SceneRoutine(string sceneName)
    {
        // The driving scene owns the player and camera itself. Skipping the
        // normal scene routine also avoids a false "player not found" warning.
        if (CarDriveSession.Active)
            yield break;

        yield return WaitForPlayer(5f);

        if (CarDriveSession.Active)
            yield break;

        if (sceneName == apartmentScene)
        {
            if (lightsObjective == null)
                lightsObjective = FindAnyObjectByType<LightsObjective>();

            yield return ApartmentRoutine();
        }
        else if (sceneName == cityScene)
        {
            CityArrival();
        }
        else if (!string.IsNullOrEmpty(storeScene) && sceneName == storeScene)
        {
            yield return StoreRoutine();
        }
    }

    private IEnumerator WaitForPlayer(float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (InteractionUtil.FindPlayer() != null &&
                InteractionUtil.FindCamera() != null)
            {
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning(
            "StoryDirector: player or camera was not found within " +
            timeout + " seconds.",
            this
        );
    }

    #region Apartment

    private IEnumerator ApartmentRoutine()
    {
        if (apartmentIntroHandled)
            yield break;

        apartmentIntroHandled = true;

        if (playApartmentOpening &&
            apartmentOpening != null &&
            apartmentOpening.HasContent)
        {
            yield return PlayDialogueRoutine(
                apartmentOpening,
                apartmentIntroDelay
            );
        }
        else if (apartmentIntroDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(apartmentIntroDelay);
        }

        if (objectiveActivationDelay > 0f)
            yield return new WaitForSecondsRealtime(objectiveActivationDelay);

        ActivateObjective(lightsObjectiveId, lightsObjectiveText);

        if (lightsObjective == null)
        {
            Debug.LogWarning(
                "StoryDirector: no LightsObjective in the apartment scene, so " +
                "objective 1 can never complete.",
                this
            );
            yield break;
        }

        lightsObjective.Activate();
    }

    /// <summary>Called by LightsObjective exactly once.</summary>
    public void NotifyLightsObjectiveCompleted()
    {
        if (lightsCompletedHandled || lightsSequenceRunning)
            return;

        StartCoroutine(LightsRoutine());
    }

    private IEnumerator LightsRoutine()
    {
        lightsSequenceRunning = true;

        ObjectiveManager.Ensure().CompleteObjective(lightsObjectiveId);

        // Optional act cue. If the cue has no clip, the apartment theme keeps
        // playing instead of falling silent.
        MusicManager.PlayCue(MusicCueIds.HomeLightsOff);

        if (lightsNotificationDelay > 0f)
            yield return new WaitForSecondsRealtime(lightsNotificationDelay);

        if (playLightsCompletedDialogue &&
            lightsCompletedDialogue != null &&
            lightsCompletedDialogue.HasContent)
        {
            yield return PlayDialogueRoutine(lightsCompletedDialogue, 0f);
        }

        ActivateObjective(goToCarObjectiveId, goToCarObjectiveText);

        lightsCompletedHandled = true;
        lightsSequenceRunning = false;
    }

    #endregion

    #region City and store

    private void CityArrival()
    {
        ObjectiveManager manager = ObjectiveManager.Instance;

        if (manager != null && manager.HasActive(goToCarObjectiveId))
            manager.CompleteObjective(goToCarObjectiveId);
    }

    /// <summary>Called by CarInteraction when the car dialogue ends.</summary>
    public void NotifyCarTripStarted()
    {
        pendingStoreTrip = true;
    }

    private IEnumerator StoreRoutine()
    {
        if (storeIntroHandled)
            yield break;

        storeIntroHandled = true;

        bool intended = pendingStoreTrip || !requireCarTripForStoreCompletion;

        if (intended)
            ObjectiveManager.Ensure().CompleteObjective(driveObjectiveId);

        if (storeOpening != null && storeOpening.HasContent)
            yield return PlayDialogueRoutine(storeOpening, storeIntroDelay);
        else if (storeIntroDelay > 0f)
            yield return new WaitForSecondsRealtime(storeIntroDelay);

        ActivateObjective(collectObjectiveId, collectObjectiveText);

        if (!string.IsNullOrEmpty(storeInstruction))
            ToastUI.Show(storeInstruction, storeInstructionDuration);

        if (autoOpenQuestLogInStore)
        {
            if (autoOpenQuestLogDelay > 0f)
                yield return new WaitForSecondsRealtime(autoOpenQuestLogDelay);

            // This is an onboarding hint for the shopping stage. If the delay
            // expires while the player is already paying (or after checkout),
            // opening the log afterwards would sit on top of the pause menu
            // and swallow ESC. Only queue it while shopping is still current.
            bool shoppingFinished = IsShoppingStageFinished();

            if (!shoppingFinished && QuestLogUI.Instance != null)
            {
                QuestLogUI.Instance.QueueAutoOpen();
            }
            else if (!shoppingFinished)
            {
                Debug.LogWarning(
                    "StoryDirector: auto-open is enabled but no QuestLogUI " +
                    "exists in the game.",
                    this
                );
            }
        }

        pendingStoreTrip = false;
    }

    private bool IsShoppingStageFinished()
    {
        ShoppingCart current = cart != null ? cart : ShoppingCart.Instance;

        if (current != null && current.IsComplete)
            return true;

        ObjectiveManager manager = ObjectiveManager.Instance;

        if (manager == null)
            return false;

        return manager.IsDone(collectObjectiveId) ||
               manager.HasActive(payObjectiveId) ||
               manager.IsDone(payObjectiveId);
    }

    #endregion

    #region Shopping

    private void OnCartChanged()
    {
        CheckFirstPickup();
    }

    private void OnCartCompleted()
    {
        if (allCollectedHandled)
            return;

        StartCoroutine(AllCollectedRoutine());
    }

    /// <summary>Called by GroceryItem after a successful pickup.</summary>
    public void NotifyItemCollected()
    {
        CheckFirstPickup();
    }

    private void CheckFirstPickup()
    {
        if (firstPickupHandled)
            return;

        ShoppingCart current = cart != null ? cart : ShoppingCart.Instance;

        if (current == null || current.TotalCollected < 1)
            return;

        firstPickupHandled = true;

        if (playFirstPickupDialogue &&
            firstPickupDialogue != null &&
            firstPickupDialogue.HasContent)
        {
            StartCoroutine(PlayDialogueRoutine(firstPickupDialogue, 0.15f));
        }
    }

    private IEnumerator AllCollectedRoutine()
    {
        if (allCollectedHandled)
            yield break;

        allCollectedHandled = true;

        ObjectiveManager.Ensure().CompleteObjective(collectObjectiveId);

        // Optional cue for the walk to the checkout.
        MusicManager.PlayCue(MusicCueIds.StoreReadyToPay);

        if (playAllCollectedDialogue &&
            allCollectedDialogue != null &&
            allCollectedDialogue.HasContent)
        {
            yield return PlayDialogueRoutine(allCollectedDialogue, 0.25f);
        }

        ActivateObjective(payObjectiveId, payObjectiveText);
    }

    #endregion

    private void ActivateObjective(string id, string text)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(text))
            return;

        ObjectiveManager.Ensure().SetObjective(id, text);
    }

    private IEnumerator PlayDialogueRoutine(
        DialogueSource source,
        float delay)
    {
        if (source == null || !source.HasContent)
            yield break;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        DialogueSystem system = DialogueSystem.Ensure();
        bool done = false;

        system.Play(source, () => done = true);

        while (!done && system != null)
            yield return null;
    }

    #region Public testing helpers

    /// <summary>
    /// Restarts the whole story. Call with true to reload the apartment scene,
    /// or false to reset state while staying where you are.
    /// </summary>
    public void ResetSession(bool reloadApartmentScene)
    {
        StopAllCoroutines();

        apartmentIntroHandled = false;
        lightsSequenceRunning = false;
        lightsCompletedHandled = false;
        storeIntroHandled = false;
        firstPickupHandled = false;
        allCollectedHandled = false;
        pendingStoreTrip = false;

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ClearAll();

        if (ShoppingCart.Instance != null)
            ShoppingCart.Instance.ResetProgress();

        if (QuestLogUI.Instance != null)
            QuestLogUI.Instance.Close();

        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.StopImmediately();

        ToastUI.HideCurrent();
        UIState.ClearModals();
        PlayerControl.ForceUnlockAll();

        // A reset without a scene reload should return to the current act
        // theme instead of leaving the scary checkout cue running.
        MusicManager.PlaySceneMusic(SceneManager.GetActiveScene().name);

        // A reset can happen while the driving scene is loaded. Drop the
        // pending return trip and any one-shot return helper, otherwise the
        // apartment intro would be skipped or a later City load would
        // teleport the player unexpectedly.
        CarDriveSession.Clear();

        CarDriveReturnApplier[] appliers =
            FindObjectsByType<CarDriveReturnApplier>();

        for (int i = 0; i < appliers.Length; i++)
        {
            if (appliers[i] != null)
                Destroy(appliers[i].gameObject);
        }

        if (PlayerInteractor.Instance != null)
            PlayerInteractor.Instance.SetSuspended(false);

        if (lightsObjective != null)
            lightsObjective.ResetObjective();

        CashierInteraction[] cashiers = FindObjectsByType<CashierInteraction>();

        for (int i = 0; i < cashiers.Length; i++)
            cashiers[i].ResetCheckout();

        if (reloadApartmentScene && !string.IsNullOrEmpty(apartmentScene))
            SceneManager.LoadScene(apartmentScene);
    }

    /// <summary>
    /// Testing shortcut: jumps to the store beats. Loads the store scene when
    /// one is configured, otherwise plays them in the current scene.
    /// </summary>
    public void SkipToStoreStage()
    {
        pendingStoreTrip = true;

        // A debug jump from the driving scene must not leave StoryDirector in
        // its driving-skip branch when the store scene loads.
        CarDriveSession.Clear();

        if (PlayerInteractor.Instance != null)
            PlayerInteractor.Instance.SetSuspended(false);

        if (!string.IsNullOrEmpty(storeScene) &&
            Application.CanStreamedLevelBeLoaded(storeScene))
        {
            SceneManager.LoadScene(storeScene);
            return;
        }

        StopAllCoroutines();

        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.StopImmediately();

        ToastUI.HideCurrent();

        // The fallback runs the store beats in the current scene, so play the
        // store theme explicitly. When a store scene is configured the scene
        // binding handles it after the load.
        MusicManager.PlayCue(MusicCueIds.Store);

        storeIntroHandled = false;
        StartCoroutine(StoreRoutine());
    }

    /// <summary>Testing shortcut: collects every required grocery.</summary>
    public void FillCartForTesting()
    {
        ShoppingCart current = ShoppingCart.Instance;

        if (current == null)
        {
            Debug.LogWarning("StoryDirector: no ShoppingCart to fill.");
            return;
        }

        if (current.Items == null)
            return;

        for (int i = 0; i < current.Items.Count; i++)
        {
            ShoppingItem item = current.Items[i];

            if (item == null)
                continue;

            while (item.collectedQuantity < item.requiredQuantity)
            {
                if (!current.TryCollect(item.id, out ShoppingItem ignored))
                    break;
            }
        }
    }

    #endregion

    private void Update()
    {
        if (cart == null)
            ResolveReferences();

        if (!enableDebugKeys)
            return;

        if (Input.GetKeyDown(KeyCode.F8))
            ResetSession(true);

        if (Input.GetKeyDown(KeyCode.F9))
            SkipToStoreStage();

        if (Input.GetKeyDown(KeyCode.F10))
            FillCartForTesting();
    }
}
