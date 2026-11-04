using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// The car in the exterior scene. Press E: play the car conversation, set the
/// drive objective, then reuse TeleportSequence to travel to the store.
/// </summary>
public class CarInteraction : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float range = 4f;
    [SerializeField] private string prompt = "E - Interact";
    [SerializeField] private int priority;
    [SerializeField] private Transform interactionPoint;

    [Header("Dialogue")]
    [SerializeField] private DialogueSource dialogue = new DialogueSource
    {
        conversationId = "CAR_Interaction_01",
        lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = "You",
                text = "Now, where's that list? I need to make sure I get everything."
            }
        }
    };

    [Header("Objective")]
    [SerializeField] private string objectiveId = "drive_to_store";

    [SerializeField]
    private string objectiveText = "Drive to the grocery store";

    [Tooltip("Optional objective completed when the car is used.")]
    [SerializeField] private string completeObjectiveId = "";

    [Header("Driving Scene")]
    [Tooltip("When on, E enters the driving scene instead of the store trip.")]
    [SerializeField] private bool useDrivingScene;

    [Tooltip("Scene name exactly as it appears in Build Settings.")]
    [SerializeField] private string drivingScene = "";

    [Header("Trip")]
    [Tooltip("Optional override. Leave empty to use StoryDirector's store scene.")]
    [SerializeField] private string destinationScene = "";

    [SerializeField] private string spawnPointName = "";
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    [SerializeField] private Vector3 spawnEulerRotation = Vector3.zero;
    [SerializeField] private Vector3 playerScale = Vector3.one;

    [Tooltip(
        "Off: use the store scene's player position, or a named spawn point. " +
        "On: force the exact coordinates below."
    )]
    [SerializeField] private bool useExactSpawn;

    [SerializeField] private float departureDelay = 0.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueFinished;
    [SerializeField] private UnityEvent onDrive;

    private bool busy;
    private bool departed;

    public KeyCode InteractionKey => interactionKey;
    public string InteractionPrompt => prompt;
    public float InteractionRange => range;
    public int InteractionPriority => priority;
    public bool CanInteract => !busy && !departed && isActiveAndEnabled;

    public Transform InteractionPoint => interactionPoint != null
        ? interactionPoint
        : transform;

    private void OnEnable()
    {
        InteractionRegistry.Register(this);
    }

    private void OnDisable()
    {
        InteractionRegistry.Unregister(this);
    }

    public void Interact()
    {
        if (busy || departed)
            return;

        busy = true;
        DialogueSystem.Ensure().Play(dialogue, OnDialogueFinished);
    }

    private void OnDialogueFinished()
    {
        ObjectiveManager manager = ObjectiveManager.Ensure();

        if (!string.IsNullOrEmpty(completeObjectiveId))
            manager.CompleteObjective(completeObjectiveId);

        if (!string.IsNullOrEmpty(objectiveText))
            manager.SetObjective(objectiveId, objectiveText);

        onDialogueFinished?.Invoke();

        StartCoroutine(DepartRoutine());
    }

    private IEnumerator DepartRoutine()
    {
        if (departureDelay > 0f)
            yield return new WaitForSecondsRealtime(departureDelay);

        onDrive?.Invoke();

        if (useDrivingScene)
        {
            if (string.IsNullOrEmpty(drivingScene) ||
                !Application.CanStreamedLevelBeLoaded(drivingScene))
            {
                Debug.LogWarning(
                    "CarInteraction: driving scene '" + drivingScene +
                    "' is empty or not in Build Settings.",
                    this
                );

                busy = false;
                yield break;
            }

            Transform drivePlayer =
                TeleportSequence.FindScenePlayer("Player");

            if (drivePlayer == null)
                drivePlayer = InteractionUtil.FindPlayer();

            if (drivePlayer == null)
            {
                Debug.LogError(
                    "CarInteraction: no player found to enter the car.",
                    this
                );

                busy = false;
                yield break;
            }

            departed = true;
            CarDriveSession.Begin(
                SceneManager.GetActiveScene().name,
                transform,
                drivePlayer
            );

            SceneManager.LoadScene(drivingScene);
            yield break;
        }

        string scene = destinationScene;

        if (string.IsNullOrEmpty(scene) && StoryDirector.Instance != null)
            scene = StoryDirector.Instance.StoreScene;

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning(
                "CarInteraction: no destination scene set and StoryDirector " +
                "has no store scene. Use the onDrive event or set a scene.",
                this
            );

            busy = false;
            yield break;
        }

        Transform player = TeleportSequence.FindScenePlayer("Player");

        if (player == null)
            player = InteractionUtil.FindPlayer();

        if (player == null)
        {
            Debug.LogError(
                "CarInteraction: no player found to travel to the store.",
                this
            );

            busy = false;
            yield break;
        }

        departed = true;

        TeleportConfig config = new TeleportConfig();
        config.destinationScene = scene;
        config.loadingMessage = "Driving";

        config.spawnMode = useExactSpawn
            ? Teleport_Scenes.SpawnMode.ExactCoordinates
            : Teleport_Scenes.SpawnMode.NamedSpawnPoint;

        config.spawnPointName = spawnPointName;
        config.spawnPosition = spawnPosition;
        config.spawnEulerRotation = spawnEulerRotation;
        config.playerScale = playerScale;

        if (StoryDirector.Instance != null)
            StoryDirector.Instance.NotifyCarTripStarted();

        TeleportSequence.Begin(config, player, OnTeleportFailed);
    }

    private void OnTeleportFailed()
    {
        busy = false;
        departed = false;
    }
}
