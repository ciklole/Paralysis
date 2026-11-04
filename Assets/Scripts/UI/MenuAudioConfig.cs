using UnityEngine;

/// <summary>
/// Inspector-editable sounds for the menus. When a clip is left empty the
/// player can synthesize a quiet placeholder tone instead of relying on an
/// external download.
/// </summary>
[System.Serializable]
public class MenuAudioConfig
{
    [Header("Clips")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Behaviour")]
    [Range(0f, 1f)]
    public float volume = 0.3f;

    public bool enableHoverSound = true;
    public bool enableClickSound = true;

    [Tooltip(
        "Generate a short, quiet electronic tone when the matching clip is " +
        "empty. Disable this if you want silence until you assign clips.")]
    public bool usePlaceholderSounds = true;
}
