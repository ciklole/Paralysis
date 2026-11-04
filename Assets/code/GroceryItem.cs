using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A grocery on a shelf. Registered with the central PlayerInteractor, so it
/// only reacts when the player is actually aiming at it.
/// </summary>
public class GroceryItem : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [Tooltip("Must match a stable id on the ShoppingCart list.")]
    [SerializeField] private string itemId = "bread";

    [Tooltip("Optional name shown in prompts / logs. Leave empty to use the list name.")]
    [SerializeField] private string displayNameOverride = "";

    [SerializeField] private Transform interactionPoint;

    [Header("Interaction")]
    [SerializeField] private KeyCode pickupKey = KeyCode.F;
    [SerializeField] private float range = 3.5f;
    [SerializeField] private string prompt = "F - Add to cart";
    [SerializeField] private int priority;

    [Header("Result")]
    [SerializeField] private bool hideAfterPickup = true;
    [SerializeField] private AudioClip pickupSoundOverride;
    [SerializeField] private UnityEvent onCollected;

    [Header("Not On The List")]
    [SerializeField] private DialogueSource notNeededDialogue = new DialogueSource
    {
        conversationId = "STORE_NotNeeded_01",
        lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = "You",
                text = "I don't think I need that."
            }
        }
    };

    [SerializeField] private float notNeededCooldown = 2f;

    private bool collected;
    private float lastRejectedAt = -10f;
    private AudioSource audioSource;

    public KeyCode InteractionKey => pickupKey;
    public string InteractionPrompt => prompt;
    public float InteractionRange => range;
    public int InteractionPriority => priority;

    public bool CanInteract
    {
        get
        {
            if (collected || !isActiveAndEnabled)
                return false;

            ShoppingCart cart = ShoppingCart.Instance;

            if (cart == null)
                return true;

            ShoppingItem item = cart.Find(itemId);

            // A required item that is already full should stop prompting.
            return item == null ||
                   item.collectedQuantity < item.requiredQuantity;
        }
    }

    public Transform InteractionPoint => interactionPoint != null
        ? interactionPoint
        : transform;

    public string ItemId => itemId;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(displayNameOverride))
                return displayNameOverride;

            ShoppingCart cart = ShoppingCart.Instance;
            ShoppingItem item = cart != null ? cart.Find(itemId) : null;

            return item != null ? item.displayName : itemId;
        }
    }

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
    }

    public void Interact()
    {
        if (collected)
            return;

        ShoppingCart cart = ShoppingCart.Instance;

        if (cart == null)
        {
            Debug.LogWarning(
                "GroceryItem: no ShoppingCart exists in the game.",
                this
            );
            return;
        }

        if (!cart.IsRequired(itemId))
        {
            ShowNotNeededLine();
            return;
        }

        if (!cart.TryCollect(itemId, out ShoppingItem item))
            return;

        collected = true;

        AudioClip clip = pickupSoundOverride != null
            ? pickupSoundOverride
            : item != null ? item.pickupSound : null;

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);

        onCollected?.Invoke();

        if (StoryDirector.Instance != null)
            StoryDirector.Instance.NotifyItemCollected();

        if (hideAfterPickup)
            gameObject.SetActive(false);
    }

    private void ShowNotNeededLine()
    {
        if (Time.unscaledTime < lastRejectedAt + notNeededCooldown)
            return;

        lastRejectedAt = Time.unscaledTime;

        if (notNeededDialogue != null && notNeededDialogue.HasContent)
            DialogueSystem.Ensure().Play(notNeededDialogue);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
