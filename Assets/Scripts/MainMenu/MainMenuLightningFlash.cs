using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Occasional lightning strike on the main menu. The silhouette behind the
/// menu is normally almost invisible; every few seconds it lights up sharply
/// and fades back to exactly where it was.
///
/// The component is added by MainMenuController, so it works with both the
/// hand-authored canvas in MainMenu.unity and the runtime-built fallback.
/// </summary>
public class MainMenuLightningFlash : MonoBehaviour
{
    [SerializeField] private Image background;

    [Header("Timing")]
    [Tooltip("Shortest wait between two strikes, in seconds.")]
    [SerializeField] private float minInterval = 10f;

    [Tooltip("Longest wait between two strikes, in seconds.")]
    [SerializeField] private float maxInterval = 15f;

    [Header("Flash")]
    [Tooltip("How opaque the background gets at the peak of a strike.")]
    [SerializeField] private float peakAlpha = 0.85f;

    [Tooltip("Colour the background reaches at the peak of a strike.")]
    [SerializeField] private Color peakColor = new Color(0.88f, 0.93f, 1f, 1f);

    [Tooltip("Time for the background to jump to full brightness.")]
    [SerializeField] private float riseTime = 0.05f;

    [Tooltip("Short dip between the two strikes of a single flash.")]
    [SerializeField] private float dipTime = 0.04f;

    [Tooltip("Time for the second strike to reach full brightness.")]
    [SerializeField] private float secondRiseTime = 0.05f;

    [Tooltip("Time for the background to fade back to normal.")]
    [SerializeField] private float fallTime = 0.45f;

    private Color baseColor;
    private bool hasBase;
    private bool striking;
    private float strikeStart;
    private float nextStrikeAt;

    public Image Background => background;

    public bool IsStriking => striking;

    private void Awake()
    {
        CaptureBase();
        ScheduleNextStrike();
    }

    private void OnEnable()
    {
        if (!hasBase)
            CaptureBase();

        ScheduleNextStrike();
    }

    private void Update()
    {
        if (background == null)
            return;

        if (!striking)
        {
            if (Time.unscaledTime >= nextStrikeAt)
                TriggerFlash();

            return;
        }

        float elapsed = Time.unscaledTime - strikeStart;

        if (elapsed >= TotalDuration)
        {
            // Put the background back exactly the way it was.
            striking = false;
            background.color = baseColor;
            ScheduleNextStrike();
            return;
        }

        background.color = ApplyIntensity(Evaluate(elapsed));
    }

    /// <summary>
    /// Points the flash at the background image it should light up.
    /// </summary>
    public void SetBackground(Image value)
    {
        background = value;
        CaptureBase();
    }

    /// <summary>
    /// Plays a strike right now instead of waiting for the next one.
    /// </summary>
    public void TriggerFlash()
    {
        if (background == null)
            return;

        if (!hasBase)
            CaptureBase();

        strikeStart = Time.unscaledTime;
        striking = true;
    }

    private float TotalDuration =>
        riseTime + dipTime + secondRiseTime + fallTime;

    private void ScheduleNextStrike()
    {
        float min = Mathf.Max(0.5f, minInterval);
        float max = Mathf.Max(min, maxInterval);

        nextStrikeAt = Time.unscaledTime + Random.Range(min, max);
    }

    private void CaptureBase()
    {
        if (background == null)
            return;

        baseColor = background.color;
        hasBase = true;
    }

    /// <summary>
    /// Lightning envelope: a sharp rise, a short dip, a second rise and a
    /// slow fade back to nothing.
    /// </summary>
    private float Evaluate(float elapsed)
    {
        float firstRise = riseTime;
        float dip = firstRise + dipTime;
        float secondRise = dip + secondRiseTime;

        if (elapsed < firstRise)
        {
            return riseTime > 0f ? elapsed / riseTime : 1f;
        }

        if (elapsed < dip)
        {
            float t = dipTime > 0f ? (elapsed - firstRise) / dipTime : 1f;
            return Mathf.Lerp(1f, 0.35f, t);
        }

        if (elapsed < secondRise)
        {
            float t = secondRiseTime > 0f
                ? (elapsed - dip) / secondRiseTime
                : 1f;

            return Mathf.Lerp(0.35f, 1f, t);
        }

        if (fallTime <= 0f)
            return 0f;

        float fall = (elapsed - secondRise) / fallTime;
        return Mathf.Clamp01(1f - fall);
    }

    private Color ApplyIntensity(float intensity)
    {
        Color color = Color.Lerp(baseColor, peakColor, intensity);
        color.a = Mathf.Lerp(baseColor.a, peakAlpha, intensity);
        return color;
    }
}
