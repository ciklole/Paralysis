using UnityEngine;

/// <summary>
/// Starts a conversation when the player presses the interaction key while
/// aiming at this object. Lines are edited in the Inspector.
/// </summary>
public class DialogueTrigger : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private DialogueSource dialogue;

    [Header("Interaction")]
    [SerializeField] private KeyCode triggerKey = KeyCode.F;
    [SerializeField] private float range = 4f;
    [SerializeField] private string prompt = "F - Talk";
    [SerializeField] private int priority;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool onlyOnce;

    private bool used;

    public KeyCode InteractionKey => triggerKey;
    public string InteractionPrompt => prompt;
    public float InteractionRange => range;
    public int InteractionPriority => priority;

    public bool CanInteract =>
        (!used || !onlyOnce) &&
        isActiveAndEnabled &&
        dialogue != null &&
        dialogue.HasContent;

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
        if (dialogue == null || !dialogue.HasContent)
            return;

        if (onlyOnce)
            used = true;

        DialogueSystem.Ensure().Play(dialogue);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
