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

    [Tooltip("Sets QualitySettings.vSyncCount to 0 so the FPS cap is used.")]
    public bool disableVSync = true;

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
    }
}
