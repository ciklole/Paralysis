using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>How a line affects player movement while it is on screen.</summary>
public enum MovementLockMode
{
    Inherit,
    Lock,
    Unlock
}

/// <summary>One line of a conversation. Everything here is Inspector-editable.</summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("Who is talking. Leave empty to hide the name.")]
    public string speaker;

    [Tooltip(
        "Optional colour for this speaker. Clear alpha uses the speaker " +
        "palette in DialogueSystem."
    )]
    public Color speakerColor = Color.clear;

    [TextArea(2, 6)]
    public string text;

    [Tooltip("Optional portrait shown while this line is on screen.")]
    public Sprite portrait;

    [Tooltip("Voice clip played when this line appears.")]
    public AudioClip voiceClip;

    [Range(0f, 1f)]
    [Tooltip("Per-line voice volume multiplier.")]
    public float voiceVolume = 1f;

    [Tooltip("Optional sound effect played when the line appears.")]
    public AudioClip soundEffect;

    /// <summary>
    /// Runtime copies must never share editable fields with the asset, so a
    /// per-line system (for example the cashier's colour pass) cannot change
    /// the authored conversation for later playthroughs.
    /// </summary>
    public DialogueLine Clone()
    {
        return (DialogueLine)MemberwiseClone();
    }

    [Tooltip("Seconds to wait after the previous click before this line appears.")]
    [Min(0f)]
    public float delayBefore;

    [Tooltip("When off, the global typewriter setting is used.")]
    public bool overrideTypewriter;

    public bool useTypewriter = true;

    [Tooltip("Seconds after the line finishes before the next click is accepted.")]
    [Min(0f)]
    public float pauseAfter = 0.25f;

    [Tooltip("Lock or unlock movement just for this line.")]
    public MovementLockMode movementLock = MovementLockMode.Inherit;

    [Tooltip("Fired once, when this line has been fully displayed.")]
    public UnityEvent onLineFinished;

    public bool UsesTypewriter(bool globalDefault)
    {
        return overrideTypewriter ? useTypewriter : globalDefault;
    }
}

/// <summary>
/// Optional reusable conversation asset. Create one from
/// Assets > Create > Paralysis > Dialogue Sequence.
/// </summary>
[CreateAssetMenu(
    menuName = "Paralysis/Dialogue Sequence",
    fileName = "DialogueSequence")]
public class DialogueSequence : ScriptableObject
{
    [Tooltip("Stable name for this conversation, e.g. APT_Opening_01.")]
    public string conversationId;

    public bool lockPlayerMovement = true;

    public DialogueLine[] lines;

    public bool HasLines => lines != null && lines.Length > 0;
}

/// <summary>
/// A conversation can come from a DialogueSequence asset or from lines typed
/// directly into the component. The asset wins when it has content.
/// </summary>
[Serializable]
public class DialogueSource
{
    [Tooltip("Optional asset. Leave empty to use the lines below.")]
    public DialogueSequence sequence;

    [Tooltip("Id used by events/logs when no sequence asset is assigned.")]
    public string conversationId;

    public bool lockPlayerMovement = true;

    [SerializeField]
    public DialogueLine[] lines;

    // Sequence assets are shared data. Runtime code (for example the cashier
    // painting its later lines red) must not mutate the asset, so each source
    // keeps one private copy for the session.
    [NonSerialized] private DialogueLine[] runtimeLines;
    [NonSerialized] private DialogueSequence runtimeLinesOwner;

    public DialogueLine[] ResolveLines()
    {
        DialogueDatabase database = DialogueDatabase.Load();
        DialogueSequence databaseSequence = database != null
            ? database.Resolve(ResolveId())
            : null;

        DialogueSequence resolved = databaseSequence != null
            ? databaseSequence
            : sequence;

        if (resolved != null && resolved.HasLines)
        {
            if (runtimeLines == null || runtimeLinesOwner != resolved)
            {
                runtimeLines = CloneLines(resolved.lines);
                runtimeLinesOwner = resolved;
            }

            return runtimeLines;
        }

        return lines;
    }

    public bool ResolveLock()
    {
        DialogueDatabase database = DialogueDatabase.Load();
        DialogueSequence databaseSequence = database != null
            ? database.Resolve(ResolveId())
            : null;

        if (databaseSequence != null)
            return databaseSequence.lockPlayerMovement;

        return sequence != null
            ? sequence.lockPlayerMovement
            : lockPlayerMovement;
    }

    private static DialogueLine[] CloneLines(DialogueLine[] source)
    {
        if (source == null)
            return null;

        DialogueLine[] copy = new DialogueLine[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            DialogueLine line = source[i];
            copy[i] = line != null
                ? line.Clone()
                : null;
        }

        return copy;
    }

    public string ResolveId()
    {
        if (sequence != null && !string.IsNullOrEmpty(sequence.conversationId))
            return sequence.conversationId;

        return conversationId;
    }

    public bool HasContent
    {
        get
        {
            DialogueLine[] resolved = ResolveLines();
            return resolved != null && resolved.Length > 0;
        }
    }
}
