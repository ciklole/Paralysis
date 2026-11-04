using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

/// <summary>
/// One named piece of music. Story code asks for a cue id, so tracks can be
/// replaced in the Inspector without touching any scene or script.
/// </summary>
[Serializable]
public class MusicCue
{
    [Tooltip(
        "Stable id used by story code and scene bindings. Keep the built-in " +
        "ids (menu, start, city, car_arc, store, scary_checkout) unchanged.")]
    public string id = "";

    [Tooltip("Friendly name shown in the Inspector.")]
    public string displayName = "";

    [TextArea(1, 3)]
    [Tooltip("Optional note about when this cue is used.")]
    public string description = "";

    [Tooltip("The music file. Leave empty to keep the previous music playing.")]
    public AudioClip clip;

    [Header("Playback")]
    [Range(0f, 1f)]
    public float volume = 0.65f;

    [Tooltip(
        "Loop the track until another cue replaces it. Turn this off for a " +
        "timed one-shot song that should play exactly once and then stop " +
        "(for example the grocery store theme).")]
    public bool loop = true;

    [Min(0f)]
    [Tooltip("Seconds spent fading this cue in.")]
    public float fadeIn = 1.5f;

    [Min(0f)]
    [Tooltip("Seconds spent fading the previous cue out.")]
    public float fadeOut = 1f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Tooltip("Optional one-shot sound played on top when the cue starts.")]
    public AudioClip stinger;

    [Range(0f, 1f)]
    public float stingerVolume = 0.8f;

    public bool HasClip => clip != null;

    /// <summary>
    /// True for a single-play cue. A looping cue keeps playing until another
    /// cue replaces it; a one-shot cue ends with its clip.
    /// </summary>
    public bool IsOneShot => !loop;

    public string Label =>
        string.IsNullOrEmpty(displayName) ? id : displayName;
}

/// <summary>
/// Plays a cue whenever a scene becomes active. Scenes do not need a music
/// component; the persistent MusicManager reads this list automatically.
/// </summary>
[Serializable]
public class MusicSceneBinding
{
    [Tooltip("Scene name exactly as it appears in Build Settings.")]
    public string sceneName = "";

    [Tooltip("Cue id from the Cues list above.")]
    public string cueId = "";

    [Tooltip(
        "Restart the cue even if the same cue is already playing. Useful for " +
        "scene reloads that should restart the track from the beginning.")]
    public bool restartWhenLoaded;
}

/// <summary>
/// Stable cue ids for the built-in story beats. The actual clip assigned to
/// each id is data, so replacing a track never requires a code or scene edit.
/// </summary>
public static class MusicCueIds
{
    public const string Menu = "menu";
    public const string Start = "start";
    public const string HomeLightsOff = "home_lights_off";
    public const string City = "city";
    public const string CarArc = "car_arc";
    public const string CarArrival = "car_arrival";
    public const string Store = "store";
    public const string StoreReadyToPay = "store_ready_to_pay";
    public const string StoreCheckout = "store_checkout";
    public const string ScaryCheckout = "scary_checkout";
    public const string End = "end";

    public static readonly string[] All =
    {
        Menu,
        Start,
        HomeLightsOff,
        City,
        CarArc,
        CarArrival,
        Store,
        StoreReadyToPay,
        StoreCheckout,
        ScaryCheckout,
        End
    };
}

/// <summary>
/// Data-driven music library. Put this asset at
/// Resources/Paralysis/MusicDatabase and assign clips in the Inspector.
/// MusicManager loads it automatically in every scene.
/// </summary>
[CreateAssetMenu(
    fileName = "MusicDatabase",
    menuName = "Paralysis/Music Database")]
[NoAutoStaticsCleanup]
public class MusicDatabase : ScriptableObject
{
    public const string ResourcePath = "Paralysis/MusicDatabase";

    [Header("Playback")]
    [Range(0f, 1f)]
    [Tooltip("Overall music level. Individual cues multiply into this.")]
    public float masterVolume = 0.7f;

    [Min(0f)]
    [Tooltip("Default crossfade when a cue does not override it.")]
    public float defaultCrossfade = 1.5f;

    [Tooltip("Play the matching scene binding whenever a scene loads.")]
    public bool autoPlaySceneMusic = true;

    [Tooltip(
        "When a scene has no binding, keep the current music instead of " +
        "stopping it.")]
    public bool keepMusicWhenUnbound = true;

    [Tooltip("Optional cue used for scenes without a binding.")]
    public string fallbackCueId = "";

    [Header("Cues")]
    [Tooltip(
        "Every song the game can play. Built-in ids are listed in " +
        "MusicCueIds; add more ids for extra acts or stings.")]
    public List<MusicCue> cues = new List<MusicCue>();

    [Header("Scenes")]
    [Tooltip("Scene name -> cue id. MainMenu, Home, City, CarDriving, Store.")]
    public List<MusicSceneBinding> scenes = new List<MusicSceneBinding>();

    private static MusicDatabase cached;
    private static bool lookupAttempted;

    public static MusicDatabase Load()
    {
        if (!lookupAttempted)
        {
            lookupAttempted = true;
            cached = Resources.Load<MusicDatabase>(ResourcePath);
        }

        return cached;
    }

    /// <summary>Used by editor tooling after the asset is created or edited.</summary>
    public static void ClearCache()
    {
        cached = null;
        lookupAttempted = false;
    }

    public MusicCue FindCue(string cueId)
    {
        if (string.IsNullOrEmpty(cueId) || cues == null)
            return null;

        for (int i = 0; i < cues.Count; i++)
        {
            MusicCue cue = cues[i];

            if (cue != null &&
                string.Equals(cue.id, cueId, StringComparison.OrdinalIgnoreCase))
            {
                return cue;
            }
        }

        return null;
    }

    public MusicSceneBinding FindScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || scenes == null)
            return null;

        for (int i = 0; i < scenes.Count; i++)
        {
            MusicSceneBinding binding = scenes[i];

            if (binding != null &&
                string.Equals(
                    binding.sceneName,
                    sceneName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return binding;
            }
        }

        return null;
    }

    public string FindCueIdForScene(string sceneName)
    {
        MusicSceneBinding binding = FindScene(sceneName);
        return binding != null ? binding.cueId : null;
    }

    public bool HasCue(string cueId)
    {
        return FindCue(cueId) != null;
    }

    public int AssignedClipCount()
    {
        if (cues == null)
            return 0;

        int count = 0;

        for (int i = 0; i < cues.Count; i++)
        {
            if (cues[i] != null && cues[i].clip != null)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Returns human-readable setup problems. Used by the custom Inspector and
    /// the smoke test; it never throws.
    /// </summary>
    public List<string> Validate()
    {
        List<string> issues = new List<string>();
        HashSet<string> ids = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );

        if (cues == null)
        {
            issues.Add("The Cues list is missing.");
        }
        else
        {
            for (int i = 0; i < cues.Count; i++)
            {
                MusicCue cue = cues[i];

                if (cue == null)
                {
                    issues.Add("Cue " + i + " is empty.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cue.id))
                {
                    issues.Add("Cue " + i + " has no id.");
                }
                else if (!ids.Add(cue.id))
                {
                    issues.Add("Duplicate cue id '" + cue.id + "'.");
                }

                if (cue.clip == null)
                {
                    issues.Add(
                        "Cue '" + cue.id + "' has no clip assigned yet."
                    );
                }
            }
        }

        if (scenes == null)
        {
            issues.Add("The Scenes list is missing.");
        }
        else
        {
            HashSet<string> sceneNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

            for (int i = 0; i < scenes.Count; i++)
            {
                MusicSceneBinding binding = scenes[i];

                if (binding == null)
                {
                    issues.Add("Scene binding " + i + " is empty.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.sceneName))
                {
                    issues.Add("Scene binding " + i + " has no scene name.");
                }
                else if (!sceneNames.Add(binding.sceneName))
                {
                    issues.Add(
                        "Duplicate scene binding for '" +
                        binding.sceneName + "'."
                    );
                }

                if (string.IsNullOrWhiteSpace(binding.cueId))
                {
                    issues.Add(
                        "Scene '" + binding.sceneName + "' has no cue id."
                    );
                }
                else if (!HasCue(binding.cueId))
                {
                    issues.Add(
                        "Scene '" + binding.sceneName + "' points at missing " +
                        "cue '" + binding.cueId + "'."
                    );
                }
            }
        }

        for (int i = 0; i < MusicCueIds.All.Length; i++)
        {
            if (!HasCue(MusicCueIds.All[i]))
            {
                issues.Add(
                    "Built-in cue '" + MusicCueIds.All[i] +
                    "' is missing. Story beats that use it will keep the " +
                    "current music."
                );
            }
        }

        return issues;
    }
}
