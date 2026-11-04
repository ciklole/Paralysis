using UnityEngine;

/// <summary>
/// Plays the shared menu hover/click sounds. One AudioSource is used for each
/// sound, so moving quickly across buttons can never stack copies of the same
/// clip on top of each other.
/// </summary>
public class MenuAudio : MonoBehaviour
{
    [SerializeField] private MenuAudioConfig config = new MenuAudioConfig();

    private AudioSource hoverSource;
    private AudioSource clickSource;

    private static AudioClip placeholderHover;
    private static AudioClip placeholderClick;

    public void Configure(MenuAudioConfig value)
    {
        config = value ?? new MenuAudioConfig();
        EnsureSources();
    }

    private void Awake()
    {
        EnsureSources();
    }

    private void EnsureSources()
    {
        if (hoverSource == null)
        {
            hoverSource = gameObject.AddComponent<AudioSource>();
            ConfigureSource(hoverSource);
        }

        if (clickSource == null)
        {
            clickSource = gameObject.AddComponent<AudioSource>();
            ConfigureSource(clickSource);
        }
    }

    private static void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = 0.3f;
    }

    public void PlayHover()
    {
        if (config == null || !config.enableHoverSound)
            return;

        AudioClip clip = ResolveClip(config.hoverSound, true);

        if (clip == null)
            return;

        Play(hoverSource, clip, config.volume);
    }

    public void PlayClick()
    {
        if (config == null || !config.enableClickSound)
            return;

        // The hover tail should never fight the click.
        if (hoverSource != null)
            hoverSource.Stop();

        AudioClip clip = ResolveClip(config.clickSound, false);

        if (clip == null)
            return;

        Play(clickSource, clip, config.volume);
    }

    private static void Play(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        // Restart the single source instead of spawning another copy.
        source.Stop();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
    }

    private AudioClip ResolveClip(AudioClip assigned, bool hover)
    {
        if (assigned != null)
            return assigned;

        if (config == null || !config.usePlaceholderSounds)
            return null;

        if (hover)
        {
            if (placeholderHover == null)
            {
                placeholderHover = CreateTone(
                    "MenuHoverPlaceholder",
                    0.055f,
                    1040f,
                    1320f,
                    0.16f
                );
            }

            return placeholderHover;
        }

        if (placeholderClick == null)
        {
            placeholderClick = CreateTone(
                "MenuClickPlaceholder",
                0.09f,
                560f,
                240f,
                0.22f
            );
        }

        return placeholderClick;
    }

    /// <summary>
    /// Builds a tiny, quiet sine sweep. This keeps the menus functional and
    /// audible without adding an external audio dependency.
    /// </summary>
    private static AudioClip CreateTone(
        string name,
        float duration,
        float startFrequency,
        float endFrequency,
        float amplitude)
    {
        const int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] data = new float[samples];
        float phase = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = samples <= 1 ? 0f : (float)i / (samples - 1);
            float frequency = Mathf.Lerp(
                startFrequency,
                endFrequency,
                t
            );

            phase += 2f * Mathf.PI * frequency / sampleRate;

            // A short fade in/out avoids clicks at the start and end.
            float envelope = Mathf.Sin(Mathf.PI * t);
            data[i] = Mathf.Sin(phase) * envelope * amplitude;
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);

        return clip;
    }
}
