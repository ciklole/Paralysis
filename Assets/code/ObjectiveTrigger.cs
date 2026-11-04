using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Generic "press E here" story trigger. Sets an objective and optionally
/// completes another one. For the car, prefer CarInteraction.
/// </summary>
public class ObjectiveTrigger : MonoBehaviour, IInteractable
{
    [Header("Objective")]
    [Tooltip("Stable id. Safe to change the visible text.")]
    [SerializeField] private string objectiveId = "objective";

    [SerializeField] private string objectiveText = "";

    [Tooltip("Objective completed when this is used. Leave empty to skip.")]
    [SerializeField] private string completeObjectiveId = "";

    [Header("Interaction")]
    [SerializeField] private KeyCode triggerKey = KeyCode.E;
    [SerializeField] private float range = 4f;
    [SerializeField] private string prompt = "E - Interact";
    [SerializeField] private int priority;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool onlyOnce = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onUsed;

    private bool used;

    public KeyCode InteractionKey => triggerKey;
    public string InteractionPrompt => prompt;
    public float InteractionRange => range;
    public int InteractionPriority => priority;
    public bool CanInteract => !used && isActiveAndEnabled;

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
        if (used)
            return;

        used = true;

        ObjectiveManager manager = ObjectiveManager.Ensure();

        if (!string.IsNullOrEmpty(completeObjectiveId))
            manager.CompleteObjective(completeObjectiveId);

        if (!string.IsNullOrEmpty(objectiveText))
            manager.SetObjective(objectiveId, objectiveText);

        onUsed?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
