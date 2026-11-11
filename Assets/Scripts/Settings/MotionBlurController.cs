using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the player's motion-blur preference through URP's built-in
/// post-processing volume stack. The controller is persistent so the effect
/// survives scene changes and cameras created at runtime (the driving camera).
/// </summary>
public class MotionBlurController : MonoBehaviour
{
    private sealed class CameraState
    {
        public Camera camera;
        public bool renderPostProcessing;
    }

    private static MotionBlurController instance;

    private readonly List<CameraState> cameraStates =
        new List<CameraState>();

    private Volume volume;
    private VolumeProfile profile;
    private MotionBlur motionBlur;
    private VolumeProfile originalGlobalProfile;
    private bool originalProfileCaptured;
    private bool enabledState;
    private float nextCameraScan;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void SetEnabled(bool enabled)
    {
        Ensure().Apply(enabled);
    }

    public static bool IsActive =>
        instance != null && instance.enabledState;

    public static bool HasVolume =>
        instance != null && instance.volume != null && instance.volume.enabled;

    public static int AppliedCameraCount =>
        instance != null ? instance.cameraStates.Count : 0;

    /// <summary>
    /// Called after a scene load so cameras that were created with the scene
    /// pick up the current preference.
    /// </summary>
    public static void RefreshScene()
    {
        if (instance != null)
            instance.Apply(instance.enabledState);
    }

    private static MotionBlurController Ensure()
    {
        if (instance != null)
            return instance;

        MotionBlurController existing =
            FindAnyObjectByType<MotionBlurController>();

        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("MotionBlurController");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MotionBlurController>();

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateVolume();
    }

    private void CreateVolume()
    {
        if (volume != null)
            return;

        GameObject volumeObject = new GameObject("MotionBlurVolume");
        volumeObject.transform.SetParent(transform, false);

        volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 1f;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "RuntimeMotionBlurProfile";

        motionBlur = profile.Add<MotionBlur>(true);
        motionBlur.active = true;

        motionBlur.mode.Override(MotionBlurMode.CameraAndObjects);
        motionBlur.quality.Override(MotionBlurQuality.Medium);

        SettingsConfig config = GameSettings.Config;

        motionBlur.intensity.Override(
            config != null ? config.motionBlurIntensity : 0.35f
        );

        motionBlur.clamp.Override(
            config != null ? config.motionBlurClamp : 0.075f
        );

        volume.sharedProfile = profile;
        volume.enabled = false;
    }

    private void Apply(bool enabled)
    {
        enabledState = enabled;

        if (volume == null)
            CreateVolume();

        if (enabled)
        {
            CaptureGlobalProfile();

            if (VolumeManager.instance != null)
            {
                // The pipeline's sample profile is not part of the game's
                // look. Removing it means enabling post-processing for motion
                // blur cannot add tonemapping or a vignette to the scene.
                VolumeManager.instance.SetGlobalDefaultProfile(null);
            }

            volume.enabled = true;
            ApplyToCameras();
        }
        else
        {
            volume.enabled = false;
            RestoreCameras();
            RestoreGlobalProfile();
        }
    }

    private void CaptureGlobalProfile()
    {
        if (originalProfileCaptured || VolumeManager.instance == null)
            return;

        originalGlobalProfile =
            VolumeManager.instance.globalDefaultProfile;

        originalProfileCaptured = true;
    }

    private void RestoreGlobalProfile()
    {
        if (!originalProfileCaptured || VolumeManager.instance == null)
            return;

        VolumeManager.instance.SetGlobalDefaultProfile(
            originalGlobalProfile
        );

        originalProfileCaptured = false;
        originalGlobalProfile = null;
    }

    private void ApplyToCameras()
    {
        // The main menu is a static title screen. Leave its camera untouched
        // so the menu's authored look is not changed by post-processing.
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            RestoreCameras();
            return;
        }

        Camera[] cameras = FindObjectsByType<Camera>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];

            if (camera == null || camera.cameraType != CameraType.Game)
                continue;

            CameraState state = FindState(camera);

            if (state == null)
            {
                state = new CameraState
                {
                    camera = camera,
                    renderPostProcessing = ReadPostProcessing(camera)
                };

                cameraStates.Add(state);
            }

            UniversalAdditionalCameraData data =
                camera.GetUniversalAdditionalCameraData();

            if (data != null)
                data.renderPostProcessing = true;
        }
    }

    private void RestoreCameras()
    {
        for (int i = cameraStates.Count - 1; i >= 0; i--)
        {
            CameraState state = cameraStates[i];

            if (state == null || state.camera == null)
            {
                cameraStates.RemoveAt(i);
                continue;
            }

            UniversalAdditionalCameraData data =
                state.camera.GetUniversalAdditionalCameraData();

            if (data != null)
                data.renderPostProcessing = state.renderPostProcessing;

            cameraStates.RemoveAt(i);
        }
    }

    private bool ReadPostProcessing(Camera camera)
    {
        UniversalAdditionalCameraData data =
            camera.GetComponent<UniversalAdditionalCameraData>();

        return data != null && data.renderPostProcessing;
    }

    private CameraState FindState(Camera camera)
    {
        for (int i = 0; i < cameraStates.Count; i++)
        {
            if (cameraStates[i] != null &&
                cameraStates[i].camera == camera)
            {
                return cameraStates[i];
            }
        }

        return null;
    }

    private void Update()
    {
        if (!enabledState)
            return;

        // The driving camera is created after the scene has loaded. A slow
        // scan catches it without running a camera search every frame.
        if (Time.unscaledTime < nextCameraScan)
            return;

        nextCameraScan = Time.unscaledTime + 0.5f;
        CaptureGlobalProfile();

        if (VolumeManager.instance != null)
            VolumeManager.instance.SetGlobalDefaultProfile(null);

        ApplyToCameras();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        RestoreCameras();
        RestoreGlobalProfile();
        instance = null;
    }
}
