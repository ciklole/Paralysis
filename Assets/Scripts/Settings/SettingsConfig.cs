using UnityEngine;

/// <summary>
/// Inspector-editable ranges, defaults and options for the player settings.
/// This asset lives in Resources so the settings screen can load it without a
/// scene reference, from both the main menu and the pause menu.
/// </summary>
[CreateAssetMenu(
    fileName = "SettingsConfig",
    menuName = "Paralysis/Settings Config")]
public class SettingsConfig : ScriptableObject
{
    [Header("Mouse Sensitivity")]
    public float minMouseSensitivity = 0.1f;
    public float maxMouseSensitivity = 10f;
    public float defaultMouseSensitivity = 2f;

    [Range(0, 3)]
    public int sensitivityDecimals = 1;

    [Header("Frame Rate")]
    public int[] frameRateOptions = { 60, 120, 240 };
    public int defaultFrameRate = 60;

    [Tooltip(
        "Turn VSync on by default. When VSync is on the FPS cap is disabled " +
        "and the display refresh rate becomes the limit.")]
    public bool defaultVSync = false;

    [Header("Resolution")]
    [Tooltip("Highest width offered in the resolution selector.")]
    public int maxResolutionWidth = 3840;

    [Tooltip("Highest height offered in the resolution selector.")]
    public int maxResolutionHeight = 2160;

    [Tooltip("Lowest width offered in the resolution selector.")]
    public int minResolutionWidth = 1024;

    [Tooltip("Lowest height offered in the resolution selector.")]
    public int minResolutionHeight = 600;

    [Tooltip(
        "Seconds the player has to confirm a new resolution before it " +
        "reverts automatically.")]
    public float resolutionConfirmTimeout = 15f;

    [Header("Motion Blur")]
    public bool defaultMotionBlur = true;

    [Range(0f, 1f)]
    public float motionBlurIntensity = 0.35f;

    [Range(0f, 0.2f)]
    public float motionBlurClamp = 0.075f;

    [Header("Persistence")]
    public string settingsFileName = "settings.json";

    public float ClampSensitivity(float value)
    {
        float min = Mathf.Min(minMouseSensitivity, maxMouseSensitivity);
        float max = Mathf.Max(minMouseSensitivity, maxMouseSensitivity);

        return Mathf.Clamp(value, min, max);
    }

    public int SanitizeFrameRate(int value)
    {
        if (frameRateOptions == null || frameRateOptions.Length == 0)
            return Mathf.Max(1, value);

        for (int i = 0; i < frameRateOptions.Length; i++)
        {
            if (frameRateOptions[i] == value)
                return value;
        }

        return defaultFrameRate > 0
            ? defaultFrameRate
            : frameRateOptions[0];
    }

    public int MaxResolutionWidth =>
        Mathf.Max(640, maxResolutionWidth > 0 ? maxResolutionWidth : 3840);

    public int MaxResolutionHeight =>
        Mathf.Max(360, maxResolutionHeight > 0 ? maxResolutionHeight : 2160);

    public int MinResolutionWidth =>
        Mathf.Clamp(minResolutionWidth, 320, MaxResolutionWidth);

    public int MinResolutionHeight =>
        Mathf.Clamp(minResolutionHeight, 240, MaxResolutionHeight);

    private void OnValidate()
    {
        if (maxMouseSensitivity < minMouseSensitivity)
            maxMouseSensitivity = minMouseSensitivity;

        defaultMouseSensitivity = ClampSensitivity(defaultMouseSensitivity);

        if (sensitivityDecimals < 0)
            sensitivityDecimals = 0;

        if (frameRateOptions == null || frameRateOptions.Length == 0)
            frameRateOptions = new[] { 60, 120, 240 };

        if (defaultFrameRate <= 0)
            defaultFrameRate = frameRateOptions[0];

        for (int i = 0; i < frameRateOptions.Length; i++)
        {
            if (frameRateOptions[i] <= 0)
                frameRateOptions[i] = 60;
        }

        if (maxResolutionWidth <= 0)
            maxResolutionWidth = 3840;

        if (maxResolutionHeight <= 0)
            maxResolutionHeight = 2160;

        if (minResolutionWidth <= 0)
            minResolutionWidth = 1024;

        if (minResolutionHeight <= 0)
            minResolutionHeight = 600;

        if (resolutionConfirmTimeout < 5f)
            resolutionConfirmTimeout = 5f;

        motionBlurIntensity = Mathf.Clamp01(motionBlurIntensity);
        motionBlurClamp = Mathf.Clamp(motionBlurClamp, 0f, 0.2f);
    }
}
