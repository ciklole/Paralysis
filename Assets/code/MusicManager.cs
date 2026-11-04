using System.Collections;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent, scene-independent music player. It bootstraps itself before the
/// first scene loads, reads Resources/Paralysis/MusicDatabase, and plays the
/// cue bound to each scene. Story scripts can request named cues for beats
/// such as the lights going out or the cashier transforming.
///
/// No scene needs a MusicManager component; this keeps the protected story
/// scenes untouched.
/// </summary>
[NoAutoStaticsCleanup]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private MusicDatabase database;
    private AudioSource[] sources;
    private AudioSource stingerSource;

    private int activeIndex;
    private MusicCue currentCue;
    private string currentCueId = "";
    private Coroutine fadeRoutine;

    private float volumeScale = 1f;
    private bool muted;

    private readonly HashSet<string> missingCueWarnings =
        new HashSet<string>();

    private bool missingClipSummaryLogged;
    private bool missingDatabaseWarned;
    private bool subscribed;

    public MusicDatabase Database
    {
        get
        {
            EnsureDatabase();
            return database;
        }
    }

    public string CurrentCueId => currentCueId;
    public MusicCue CurrentCue => currentCue;

    /// <summary>
    /// True while the current cue repeats until another cue replaces it.
    /// False for a one-shot (timed) cue.
    /// </summary>
    public bool CurrentCueLoops => currentCue != null && currentCue.loop;

    /// <summary>True while a timed, single-play cue is the current cue.</summary>
    public bool CurrentCueIsOneShot => currentCue != null && !currentCue.loop;
    public bool IsPlaying
    {
        get
        {
            AudioSource source = ActiveSource;
            return currentCue != null &&
                   source != null &&
                   source.clip != null &&
                   source.isPlaying;
        }
    }

    public AudioClip CurrentClip
    {
        get
        {
            AudioSource source = ActiveSource;
            return source != null ? source.clip : null;
        }
    }

    public float CurrentVolume
    {
        get
        {
            AudioSource source = ActiveSource;
            return source != null ? source.volume : 0f;
        }
    }

    public float MasterVolume
    {
        get
        {
            float master = database != null
                ? Mathf.Clamp01(database.masterVolume)
                : 1f;

            return master * Mathf.Clamp01(volumeScale);
        }
    }

    public bool IsMuted => muted;

    #region Bootstrap

    /// <summary>
    /// Domain reload is disabled in this project, so statics survive between
    /// Play Mode sessions. Clear the old instance explicitly.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Ensure();
    }

    public static MusicManager Ensure()
    {
        if (Instance != null)
            return Instance;

        MusicManager existing = FindAnyObjectByType<MusicManager>();

        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject go = new GameObject("MusicManager");
        return go.AddComponent<MusicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        MusicDatabase.ClearCache();
        EnsureDatabase();
        CreateSources();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        subscribed = true;
    }

    private void Start()
    {
        EnsureDatabase();

        if (database != null && database.autoPlaySceneMusic)
            PlaySceneMusicInternal(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        // A one-shot (timed) cue is not looped, so it ends on its own. Clear
        // it once it finishes so the next cue request starts from a clean
        // state instead of resuming a track that already played out.
        if (currentCue == null || currentCue.loop || fadeRoutine != null)
            return;

        AudioSource source = ActiveSource;

        if (source == null || source.isPlaying)
            return;

        currentCue = null;
        currentCueId = "";
        source.clip = null;
        source.volume = 0f;
    }

    private void OnDestroy()
    {
        if (subscribed)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            subscribed = false;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (Instance == this)
            Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureDatabase();

        if (database != null && database.autoPlaySceneMusic)
            PlaySceneMusicInternal(scene.name);
    }

    private void EnsureDatabase()
    {
        if (database != null)
            return;

        database = MusicDatabase.Load();

        if (database == null && !missingDatabaseWarned)
        {
            missingDatabaseWarned = true;

            Debug.LogWarning(
                "MusicManager: no MusicDatabase found at Resources/" +
                MusicDatabase.ResourcePath + ". Create one from " +
                "Assets > Create > Paralysis > Music Database and put it in " +
                "Assets/Resources/Paralysis.",
                this
            );
        }
    }

    private void CreateSources()
    {
        sources = new AudioSource[2];

        for (int i = 0; i < sources.Length; i++)
        {
            GameObject go = new GameObject("MusicSource" + (i + 1));
            go.transform.SetParent(transform, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.volume = 0f;

            sources[i] = source;
        }

        GameObject stingerObject = new GameObject("MusicStinger");
        stingerObject.transform.SetParent(transform, false);

        stingerSource = stingerObject.AddComponent<AudioSource>();
        stingerSource.playOnAwake = false;
        stingerSource.loop = false;
        stingerSource.spatialBlend = 0f;
        stingerSource.ignoreListenerPause = true;
        stingerSource.volume = 1f;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Plays a cue by id. Missing cues and cues without a clip leave the
    /// current music playing, which makes optional story beats safe.
    /// </summary>
    public static bool PlayCue(string cueId)
    {
        return Ensure().PlayCueInternal(cueId, -1f, false);
    }

    /// <summary>Plays a cue with an explicit crossfade duration.</summary>
    public static bool PlayCue(string cueId, float fadeSeconds)
    {
        return Ensure().PlayCueInternal(cueId, Mathf.Max(0f, fadeSeconds), false);
    }

    /// <summary>Plays a cue from the beginning, even if it is already playing.</summary>
    public static bool RestartCue(string cueId)
    {
        return Ensure().PlayCueInternal(cueId, -1f, true);
    }

    /// <summary>Plays the cue bound to a scene, if one exists.</summary>
    public static bool PlaySceneMusic(string sceneName)
    {
        return Ensure().PlaySceneMusicInternal(sceneName);
    }

    public static void StopMusic(float fadeSeconds = 1f)
    {
        if (Instance != null)
            Instance.StopInternal(fadeSeconds);
    }

    /// <summary>Player-facing music level, multiplied by the database level.</summary>
    public static void SetVolume(float value)
    {
        Ensure().SetVolumeScale(value);
    }

    public static void SetMuted(bool value)
    {
        Ensure().SetMutedState(value);
    }

    public void SetVolumeScale(float value)
    {
        volumeScale = Mathf.Clamp01(value);
        ApplyCurrentVolume();
    }

    public void SetMutedState(bool value)
    {
        muted = value;
        ApplyCurrentVolume();
    }

    /// <summary>Testing / replay helper: clears the current cue immediately.</summary>
    public void ResetMusic()
    {
        StopInternal(0f);
        missingCueWarnings.Clear();
        missingClipSummaryLogged = false;
    }

    #endregion

    #region Playback

    private bool PlaySceneMusicInternal(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        EnsureDatabase();

        if (database == null)
            return false;

        MusicSceneBinding binding = database.FindScene(sceneName);
        string cueId = binding != null ? binding.cueId : null;
        bool restart = binding != null && binding.restartWhenLoaded;

        if (string.IsNullOrEmpty(cueId))
        {
            if (!string.IsNullOrEmpty(database.fallbackCueId))
            {
                cueId = database.fallbackCueId;
            }
            else if (database.keepMusicWhenUnbound)
            {
                return false;
            }
            else
            {
                StopInternal(database.defaultCrossfade);
                return false;
            }
        }

        return PlayCueInternal(cueId, -1f, restart);
    }

    private bool PlayCueInternal(
        string cueId,
        float fadeOverride,
        bool restart)
    {
        if (string.IsNullOrEmpty(cueId))
            return false;

        EnsureDatabase();

        if (database == null)
            return false;

        MusicCue cue = database.FindCue(cueId);

        if (cue == null)
        {
            WarnMissingCue(cueId);
            return false;
        }

        if (cue.clip == null)
        {
            NoteMissingClip(cueId);
            return false;
        }

        AudioSource active = ActiveSource;

        if (!restart &&
            string.Equals(
                currentCueId,
                cue.id,
                System.StringComparison.OrdinalIgnoreCase) &&
            active != null &&
            active.clip == cue.clip &&
            active.isPlaying)
        {
            currentCue = cue;
            active.loop = cue.loop;
            active.pitch = cue.pitch;
            active.volume = EffectiveVolume(cue);
            return true;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        bool wasPlaying = active != null &&
                          active.clip != null &&
                          active.isPlaying;

        // Clear whichever source is not going to carry the new cue. This also
        // cleans up a fade that was interrupted by another cue request.
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];

            if (source == null)
                continue;

            if (wasPlaying && source == active)
                continue;

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        int nextIndex = wasPlaying
            ? 1 - activeIndex
            : Mathf.Clamp(activeIndex, 0, sources.Length - 1);

        AudioSource next = sources[nextIndex];

        next.Stop();
        next.clip = cue.clip;
        next.loop = cue.loop;
        next.pitch = cue.pitch;
        next.volume = 0f;
        next.Play();

        activeIndex = nextIndex;
        currentCue = cue;
        currentCueId = cue.id;

        PlayStinger(cue);

        float target = EffectiveVolume(cue);
        float fadeIn = fadeOverride >= 0f
            ? fadeOverride
            : Mathf.Max(0f, cue.fadeIn);

        float fadeOut = fadeOverride >= 0f
            ? fadeOverride
            : Mathf.Max(0f, cue.fadeOut);

        if (!wasPlaying || active == null || active == next)
        {
            next.volume = target;
            return true;
        }

        fadeRoutine = StartCoroutine(
            CrossfadeRoutine(active, next, target, fadeIn, fadeOut)
        );

        return true;
    }

    private IEnumerator CrossfadeRoutine(
        AudioSource from,
        AudioSource to,
        float target,
        float fadeIn,
        float fadeOut)
    {
        float fromStart = from != null ? from.volume : 0f;
        float duration = Mathf.Max(fadeIn, fadeOut);

        if (duration <= 0.001f)
        {
            if (from != null)
            {
                from.Stop();
                from.clip = null;
                from.volume = 0f;
            }

            if (to != null)
                to.volume = target;

            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float outT = fadeOut <= 0.001f
                ? 1f
                : Mathf.Clamp01(elapsed / fadeOut);

            float inT = fadeIn <= 0.001f
                ? 1f
                : Mathf.Clamp01(elapsed / fadeIn);

            if (from != null)
                from.volume = Mathf.Lerp(fromStart, 0f, outT);

            if (to != null)
                to.volume = Mathf.Lerp(0f, target, inT);

            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.clip = null;
            from.volume = 0f;
        }

        if (to != null)
            to.volume = target;

        fadeRoutine = null;
    }

    private void StopInternal(float fadeSeconds)
    {
        currentCue = null;
        currentCueId = "";

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (sources == null)
            return;

        if (fadeSeconds <= 0.001f)
        {
            StopSources();
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutAllRoutine(fadeSeconds));
    }

    private IEnumerator FadeOutAllRoutine(float fadeSeconds)
    {
        float[] startVolumes = new float[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            startVolumes[i] = sources[i] != null ? sources[i].volume : 0f;
        }

        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeSeconds);

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                    sources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
            }

            yield return null;
        }

        StopSources();
        fadeRoutine = null;
    }

    private void StopSources()
    {
        if (sources != null)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];

                if (source == null)
                    continue;

                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }
        }

        if (stingerSource != null)
            stingerSource.Stop();
    }

    private void PlayStinger(MusicCue cue)
    {
        if (cue == null || cue.stinger == null || stingerSource == null)
            return;

        stingerSource.Stop();
        stingerSource.volume = EffectiveVolume(cue, cue.stingerVolume);
        stingerSource.PlayOneShot(cue.stinger);
    }

    private void ApplyCurrentVolume()
    {
        if (currentCue == null)
            return;

        AudioSource source = ActiveSource;

        if (source != null)
            source.volume = EffectiveVolume(currentCue);
    }

    private float EffectiveVolume(MusicCue cue)
    {
        return EffectiveVolume(cue, cue != null ? cue.volume : 0f);
    }

    private float EffectiveVolume(MusicCue cue, float cueVolume)
    {
        float master = database != null
            ? Mathf.Clamp01(database.masterVolume)
            : 1f;

        float result = Mathf.Clamp01(cueVolume) *
                       master *
                       Mathf.Clamp01(volumeScale);

        return muted ? 0f : result;
    }

    private AudioSource ActiveSource
    {
        get
        {
            if (sources == null || sources.Length == 0)
                return null;

            int index = Mathf.Clamp(activeIndex, 0, sources.Length - 1);
            AudioSource source = sources[index];

            if (source != null && source.isPlaying)
                return source;

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i].isPlaying)
                    return sources[i];
            }

            return source;
        }
    }

    private void WarnMissingCue(string cueId)
    {
        if (!missingCueWarnings.Add(cueId))
            return;

        Debug.LogWarning(
            "MusicManager: cue '" + cueId + "' is not in " +
            MusicDatabase.ResourcePath + ". The current music will keep " +
            "playing.",
            this
        );
    }

    private void NoteMissingClip(string cueId)
    {
        if (missingClipSummaryLogged)
            return;

        missingClipSummaryLogged = true;

        int total = database != null && database.cues != null
            ? database.cues.Count
            : 0;

        int assigned = database != null
            ? database.AssignedClipCount()
            : 0;

        Debug.Log(
            "MusicManager: " + (total - assigned) + " of " + total +
            " music cues have no clip yet (first: '" + cueId + "'). " +
            "Add clips in " + MusicDatabase.ResourcePath +
            ". The current music will keep playing.",
            this
        );
    }

    #endregion
}
