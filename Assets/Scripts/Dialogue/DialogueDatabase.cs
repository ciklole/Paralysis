using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

/// <summary>
/// Central override for every conversation in the game. Each entry is a
/// DialogueSequence asset whose lines can be edited (text, speaker, voice clip,
/// portrait, timing) without touching the protected story scenes.
///
/// DialogueSource checks this database first by conversation id, then falls
/// back to its own sequence/lines. Remove an entry to restore the inline scene
/// dialogue.
/// </summary>
[CreateAssetMenu(
    fileName = "DialogueDatabase",
    menuName = "Paralysis/Dialogue Database")]
[NoAutoStaticsCleanup]
public class DialogueDatabase : ScriptableObject
{
    public const string ResourcePath = "Paralysis/DialogueDatabase";

    [Tooltip(
        "Every conversation that should be editable outside the story scenes. " +
        "Ids must match the conversationId used by the scene component.")]
    public List<DialogueSequence> sequences =
        new List<DialogueSequence>();

    [NonSerialized] private Dictionary<string, DialogueSequence> lookup;
    [NonSerialized] private bool lookupBuilt;

    private static DialogueDatabase cached;
    private static bool lookupAttempted;

    public static DialogueDatabase Load()
    {
        if (!lookupAttempted)
        {
            lookupAttempted = true;
            cached = Resources.Load<DialogueDatabase>(ResourcePath);
        }

        return cached;
    }

    public static void ClearCache()
    {
        cached = null;
        lookupAttempted = false;
    }

    public DialogueSequence Resolve(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId))
            return null;

        BuildLookup();

        DialogueSequence sequence;

        return lookup.TryGetValue(conversationId, out sequence)
            ? sequence
            : null;
    }

    public DialogueLine[] ResolveLines(string conversationId)
    {
        DialogueSequence sequence = Resolve(conversationId);

        if (sequence == null || !sequence.HasLines)
            return null;

        return sequence.lines;
    }

    public bool? ResolveLock(string conversationId)
    {
        DialogueSequence sequence = Resolve(conversationId);

        return sequence != null ? sequence.lockPlayerMovement : (bool?)null;
    }

    public void RebuildLookup()
    {
        lookupBuilt = false;
        BuildLookup();
    }

    private void BuildLookup()
    {
        if (lookupBuilt)
            return;

        lookupBuilt = true;
        lookup = new Dictionary<string, DialogueSequence>(
            StringComparer.OrdinalIgnoreCase
        );

        if (sequences == null)
            return;

        for (int i = 0; i < sequences.Count; i++)
        {
            DialogueSequence sequence = sequences[i];

            if (sequence == null ||
                string.IsNullOrEmpty(sequence.conversationId) ||
                !sequence.HasLines)
            {
                continue;
            }

            if (!lookup.ContainsKey(sequence.conversationId))
                lookup.Add(sequence.conversationId, sequence);
        }
    }

    private void OnValidate()
    {
        lookupBuilt = false;
        cached = this;
        lookupAttempted = true;
    }
}
