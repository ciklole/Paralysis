using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the two gameplay preferences requested for this project: mouse
/// sensitivity and the frame-rate cap. The values are stored separately from
/// story progress and are re-applied after every scene load.
/// </summary>
[NoAutoStaticsCleanup]
public static class GameSettings
{
    private const string ConfigResourcePath = "Paralysis/SettingsConfig";

    public static SettingsConfig Config { get; private set; }
    public static float MouseSensitivity { get; private set; } = 2f;
    public static int FrameRate { get; private set; } = 60;
    public static bool Initialized { get; private set; }

    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (Config == null)
        {
            Config = Resources.Load<SettingsConfig>(ConfigResourcePath);

            if (Config == null)
            {
                Config = ScriptableObject.CreateInstance<SettingsConfig>();
                Config.name = "RuntimeSettingsConfig";
            }
        }

        Load();

        ApplyFrameRate();
        ApplyMouseSensitivity();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        Initialized = true;
    }

    public static void EnsureInitialized()
    {
        if (!Initialized || Config == null)
            Initialize();
    }

    /// <summary>Re-reads the settings file. Used by tests and diagnostics.</summary>
    public static void Reload()
    {
        if (Config == null)
        {
            Config = Resources.Load<SettingsConfig>(ConfigResourcePath);

            if (Config == null)
                Config = ScriptableObject.CreateInstance<SettingsConfig>();
        }

        Load();
        ApplyFrameRate();
        ApplyMouseSensitivity();
        Initialized = true;
        Changed?.Invoke();
    }

    private static void Load()
    {
        PlayerSettingsData data;

        if (SaveIO.TryReadJson(SaveIO.SettingsPath, out data))
        {
            MouseSensitivity = Config.ClampSensitivity(
                data.mouseSensitivity
            );

            FrameRate = Config.SanitizeFrameRate(data.frameRate);
            return;
        }

        MouseSensitivity = Config.ClampSensitivity(
            Config.defaultMouseSensitivity
        );

        FrameRate = Config.SanitizeFrameRate(Config.defaultFrameRate);
    }

    public static void Save()
    {
        if (Config == null)
            EnsureInitialized();

        PlayerSettingsData data = new PlayerSettingsData
        {
            mouseSensitivity = MouseSensitivity,
            frameRate = FrameRate
        };

        SaveIO.WriteJson(SaveIO.SettingsPath, data);
    }

    public static void SetMouseSensitivity(float value, bool save = true)
    {
        EnsureInitialized();

        MouseSensitivity = Config.ClampSensitivity(value);
        ApplyMouseSensitivity();

        if (save)
            Save();

        Changed?.Invoke();
    }

    public static void SetFrameRate(int value, bool save = true)
    {
        EnsureInitialized();

        FrameRate = Config.SanitizeFrameRate(value);
        ApplyFrameRate();

        if (save)
            Save();

        Changed?.Invoke();
    }

    public static void ApplyFrameRate()
    {
        if (Config == null)
            return;

        if (Config.disableVSync)
            QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = FrameRate;
    }

    /// <summary>
    /// Applies the saved value to the existing first-person controller. This
    /// deliberately writes the controller's own mouseSensitivity field; there
    /// is no second look system.
    /// </summary>
    public static void ApplyMouseSensitivity()
    {
        FirstPersonController[] controllers =
            Object.FindObjectsByType<FirstPersonController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
                controllers[i].mouseSensitivity = MouseSensitivity;
        }
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        ApplyMouseSensitivity();
        ApplyFrameRate();
    }

    public static int[] FrameRateOptions
    {
        get
        {
            if (Config != null &&
                Config.frameRateOptions != null &&
                Config.frameRateOptions.Length > 0)
            {
                return Config.frameRateOptions;
            }

            return new[] { 60, 120, 240 };
        }
    }

    public static string FormatSensitivity(float value)
    {
        int decimals = Config != null ? Config.sensitivityDecimals : 1;
        return value.ToString("F" + Mathf.Clamp(decimals, 0, 3));
    }
}
