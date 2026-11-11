using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the player preferences: mouse sensitivity, frame-rate cap, VSync,
/// display resolution and motion blur. The values are stored separately from
/// story progress and are re-applied after every scene load.
/// </summary>
[NoAutoStaticsCleanup]
public static class GameSettings
{
    private const string ConfigResourcePath = "Paralysis/SettingsConfig";

    private static readonly List<Vector2Int> resolutionOptions =
        new List<Vector2Int>();

    public static SettingsConfig Config { get; private set; }
    public static float MouseSensitivity { get; private set; } = 2f;
    public static int FrameRate { get; private set; } = 60;
    public static bool VSyncEnabled { get; private set; }
    public static bool MotionBlurEnabled { get; private set; } = true;
    public static int ResolutionWidth { get; private set; }
    public static int ResolutionHeight { get; private set; }
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
        ApplyMotionBlur();
        ApplySavedResolution();

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
        ApplyMotionBlur();
        ApplySavedResolution();
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

            // Older files did not contain the graphics options. Give those
            // players the new defaults instead of silently turning everything
            // off because a missing bool deserialized as false.
            bool legacy = data.version < 2;

            VSyncEnabled = legacy ? Config.defaultVSync : data.vsync;
            MotionBlurEnabled = legacy
                ? Config.defaultMotionBlur
                : data.motionBlur;

            ResolutionWidth = legacy ? 0 : data.resolutionWidth;
            ResolutionHeight = legacy ? 0 : data.resolutionHeight;

            return;
        }

        MouseSensitivity = Config.ClampSensitivity(
            Config.defaultMouseSensitivity
        );

        FrameRate = Config.SanitizeFrameRate(Config.defaultFrameRate);
        VSyncEnabled = Config.defaultVSync;
        MotionBlurEnabled = Config.defaultMotionBlur;
        ResolutionWidth = 0;
        ResolutionHeight = 0;
    }

    public static void Save()
    {
        if (Config == null)
            EnsureInitialized();

        PlayerSettingsData data = new PlayerSettingsData
        {
            version = 2,
            mouseSensitivity = MouseSensitivity,
            frameRate = FrameRate,
            vsync = VSyncEnabled,
            motionBlur = MotionBlurEnabled,
            resolutionWidth = ResolutionGuard.Pending
                ? ResolutionGuard.PreviousWidth
                : ResolutionWidth,
            resolutionHeight = ResolutionGuard.Pending
                ? ResolutionGuard.PreviousHeight
                : ResolutionHeight
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

    public static void SetVSync(bool value, bool save = true)
    {
        EnsureInitialized();

        VSyncEnabled = value;
        ApplyFrameRate();

        if (save)
            Save();

        Changed?.Invoke();
    }

    public static void SetMotionBlur(bool value, bool save = true)
    {
        EnsureInitialized();

        MotionBlurEnabled = value;
        ApplyMotionBlur();

        if (save)
            Save();

        Changed?.Invoke();
    }

    /// <summary>
    /// VSync and the frame-rate cap are mutually exclusive by design. With
    /// VSync on the display refresh rate is the limiter, so targetFrameRate is
    /// left at -1. With VSync off the existing FPS cap is applied.
    /// </summary>
    public static void ApplyFrameRate()
    {
        if (Config == null)
            return;

        QualitySettings.vSyncCount = VSyncEnabled ? 1 : 0;
        Application.targetFrameRate = VSyncEnabled ? -1 : FrameRate;
    }

    public static void ApplyMotionBlur()
    {
        MotionBlurController.SetEnabled(MotionBlurEnabled);
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
        MotionBlurController.RefreshScene();
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

    // ------------------------------------------------------------------
    // Resolution
    // ------------------------------------------------------------------

    public static IReadOnlyList<Vector2Int> ResolutionOptions
    {
        get
        {
            if (resolutionOptions.Count == 0)
                RefreshResolutionOptions();

            return resolutionOptions;
        }
    }

    /// <summary>
    /// Rebuilds the list from the modes the current display actually reports.
    /// Duplicate refresh rates are collapsed and everything above the 4K cap
    /// is discarded.
    /// </summary>
    public static void RefreshResolutionOptions()
    {
        resolutionOptions.Clear();

        int maxWidth = Config != null ? Config.MaxResolutionWidth : 3840;
        int maxHeight = Config != null ? Config.MaxResolutionHeight : 2160;
        int minWidth = Config != null ? Config.MinResolutionWidth : 1024;
        int minHeight = Config != null ? Config.MinResolutionHeight : 600;

        Resolution[] modes = Screen.resolutions;

        if (modes != null)
        {
            for (int i = 0; i < modes.Length; i++)
            {
                AddResolutionOption(
                    modes[i].width,
                    modes[i].height,
                    minWidth,
                    minHeight,
                    maxWidth,
                    maxHeight
                );
            }
        }

        // Some platforms (notably the editor and a few Linux setups) report an
        // empty list. The current window is still a valid choice.
        AddResolutionOption(
            Screen.width,
            Screen.height,
            minWidth,
            minHeight,
            maxWidth,
            maxHeight
        );

        if (resolutionOptions.Count == 0)
        {
            AddResolutionOption(
                1920,
                1080,
                640,
                360,
                maxWidth,
                maxHeight
            );

            AddResolutionOption(
                1280,
                720,
                640,
                360,
                maxWidth,
                maxHeight
            );

            AddResolutionOption(
                1024,
                600,
                640,
                360,
                maxWidth,
                maxHeight
            );
        }

        resolutionOptions.Sort(CompareResolution);
    }

    private static void AddResolutionOption(
        int width,
        int height,
        int minWidth,
        int minHeight,
        int maxWidth,
        int maxHeight)
    {
        if (width < minWidth || height < minHeight)
            return;

        if (width > maxWidth || height > maxHeight)
            return;

        Vector2Int option = new Vector2Int(width, height);

        if (!resolutionOptions.Contains(option))
            resolutionOptions.Add(option);
    }

    private static int CompareResolution(Vector2Int a, Vector2Int b)
    {
        int widthCompare = a.x.CompareTo(b.x);

        if (widthCompare != 0)
            return widthCompare;

        return a.y.CompareTo(b.y);
    }

    public static bool IsResolutionSupported(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return false;

        IReadOnlyList<Vector2Int> options = ResolutionOptions;

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].x == width && options[i].y == height)
                return true;
        }

        return false;
    }

    public static int FindResolutionIndex(int width, int height)
    {
        IReadOnlyList<Vector2Int> options = ResolutionOptions;

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].x == width && options[i].y == height)
                return i;
        }

        return -1;
    }

    public static string FormatResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return "CURRENT";

        return width + " x " + height;
    }

    /// <summary>
    /// Applies a resolution immediately and asks the guard to show the
    /// confirmation prompt. The choice is only written to disk after the
    /// player confirms it.
    /// </summary>
    public static bool BeginResolutionChange(int width, int height)
    {
        EnsureInitialized();
        RefreshResolutionOptions();

        if (!IsResolutionSupported(width, height))
            return false;

        int previousWidth = ResolutionWidth > 0
            ? ResolutionWidth
            : Screen.width;

        int previousHeight = ResolutionHeight > 0
            ? ResolutionHeight
            : Screen.height;

        Screen.SetResolution(width, height, Screen.fullScreenMode);

        ResolutionWidth = width;
        ResolutionHeight = height;

        float timeout = Config != null
            ? Config.resolutionConfirmTimeout
            : 15f;

        ResolutionGuard.Begin(
            previousWidth,
            previousHeight,
            width,
            height,
            timeout
        );

        Changed?.Invoke();
        return true;
    }

    /// <summary>Keeps the pending resolution and saves it.</summary>
    public static void CommitResolution(int width, int height)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;
        Save();
        Changed?.Invoke();
    }

    /// <summary>Restores the resolution that was active before the change.</summary>
    public static void RevertResolution(int width, int height)
    {
        if (width > 0 && height > 0)
            Screen.SetResolution(width, height, Screen.fullScreenMode);

        ResolutionWidth = width;
        ResolutionHeight = height;
        Changed?.Invoke();
    }

    /// <summary>Moves to the next/previous supported resolution.</summary>
    public static bool CycleResolution(int direction)
    {
        EnsureInitialized();
        RefreshResolutionOptions();

        IReadOnlyList<Vector2Int> options = ResolutionOptions;

        if (options.Count == 0)
            return false;

        int index = FindResolutionIndex(
            ResolutionWidth,
            ResolutionHeight
        );

        if (index < 0)
            index = 0;

        int next = index + (direction >= 0 ? 1 : -1);

        if (next >= options.Count)
            next = 0;
        else if (next < 0)
            next = options.Count - 1;

        return BeginResolutionChange(options[next].x, options[next].y);
    }

    private static void ApplySavedResolution()
    {
        RefreshResolutionOptions();

        if (ResolutionWidth > 0 &&
            ResolutionHeight > 0 &&
            IsResolutionSupported(ResolutionWidth, ResolutionHeight))
        {
            Screen.SetResolution(
                ResolutionWidth,
                ResolutionHeight,
                Screen.fullScreenMode
            );

            return;
        }

        // A saved mode that the current display no longer reports is not
        // applied. Pick a safe supported mode instead and remember it.
        if (ResolutionWidth > 0 || ResolutionHeight > 0)
        {
            Vector2Int fallback = FindSafeFallback(
                ResolutionWidth,
                ResolutionHeight
            );

            ResolutionWidth = fallback.x;
            ResolutionHeight = fallback.y;

            Screen.SetResolution(
                fallback.x,
                fallback.y,
                Screen.fullScreenMode
            );

            Save();
            return;
        }

        ResolutionWidth = Screen.width;
        ResolutionHeight = Screen.height;
    }

    private static Vector2Int FindSafeFallback(int width, int height)
    {
        IReadOnlyList<Vector2Int> options = ResolutionOptions;

        if (options.Count == 0)
            return new Vector2Int(Screen.width, Screen.height);

        // Prefer a common 1080p mode when it is supported.
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].x == 1920 && options[i].y == 1080)
                return options[i];
        }

        // Otherwise use the largest mode that does not exceed the old size.
        long savedArea = (long)Mathf.Max(1, width) * Mathf.Max(1, height);
        Vector2Int best = options[0];

        for (int i = 0; i < options.Count; i++)
        {
            long area = (long)options[i].x * options[i].y;

            if (area <= savedArea)
                best = options[i];
        }

        return best;
    }
}
