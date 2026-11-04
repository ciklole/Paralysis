using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anything the player can press a key on. One central PlayerInteractor
/// resolves the best target, shows the prompt and calls Interact, so two
/// nearby objects can never respond to the same key press.
/// </summary>
public interface IInteractable
{
    KeyCode InteractionKey { get; }
    string InteractionPrompt { get; }
    float InteractionRange { get; }
    int InteractionPriority { get; }
    bool CanInteract { get; }
    Transform InteractionPoint { get; }
    void Interact();
}

/// <summary>Live list of everything currently interactable in the loaded scenes.</summary>
public static class InteractionRegistry
{
    private static readonly List<IInteractable> items = new List<IInteractable>();

    public static IReadOnlyList<IInteractable> Items => items;

    public static void Register(IInteractable item)
    {
        if (item == null)
            return;

        if (!items.Contains(item))
            items.Add(item);
    }

    public static void Unregister(IInteractable item)
    {
        if (item == null)
            return;

        items.Remove(item);
    }

    public static void Clear()
    {
        items.Clear();
    }
}
