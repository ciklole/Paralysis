using System.Collections;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

/// <summary>
/// Owns the store-wide red lighting change. It snapshots every light and the
/// emissive ceiling panels, cross-fades them to red, optionally flickers, and
/// restores the exact original values and material arrays afterwards.
///
/// The controller is idempotent: calling BeginRed twice cannot start a second
/// fade, and disabling/destroying it mid-sequence restores the store.
/// </summary>
[AutoStaticsCleanup]
public partial class StoreLightingController : MonoBehaviour
{
    [Header("Lights")]
    [Tooltip("Leave empty to collect every light in the loaded store scene.")]
    [SerializeField] private Light[] lights;

    [SerializeField] private bool includeDirectionalLights = true;

    [Header("Red Look")]
    [SerializeField] private Color redLightColor =
        new Color(1f, 0.035f, 0.02f, 1f);

    [SerializeField, Range(0.1f, 1.5f)] private float redIntensityScale = 0.9f;

    [SerializeField] private Color redPanelEmission =
        new Color(2.2f, 0.025f, 0.01f, 1f);

    [Header("Emissive Panels")]
    [Tooltip("Leave empty to collect the grocery light-panel material.")]
    [SerializeField] private Renderer[] lightPanels;

    [SerializeField] private string lightPanelMaterialName = "Grocery_Light_Mat";

    [Header("Flicker")]
    [SerializeField, Range(0f, 1f)] private float flickerDepth = 0.35f;

    [SerializeField, Min(1f)] private float flickerFrequency = 18f;

    private class LightState
    {
        public Light light;
        public Color color;
        public float intensity;
        public float range;
        public float spotAngle;
        public float innerSpotAngle;
        public bool enabled;
    }

    private class PanelState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public int[] replacedSlots;
    }

    private readonly List<LightState> lightStates = new List<LightState>();
    private readonly List<PanelState> panelStates = new List<PanelState>();

    private Material redPanelMaterial;
    private Coroutine routine;
    private bool captured;
    private bool redActive;

    private static StoreLightingController redOwner;

    public bool IsRed => redActive;

    private void Awake()
    {
        CollectIfNeeded();
    }

    /// <summary>
    /// Starts the smooth red transition. Returns false when another controller
    /// already owns the store lighting, so two copies can never fight.
    /// </summary>
    public bool BeginRed(float duration, float flickerDuration)
    {
        if (redOwner != null && redOwner != this)
        {
            Debug.LogWarning(
                "StoreLightingController: another lighting controller is " +
                "already active; ignoring this one.",
                this
            );
            return false;
        }

        Capture();

        if (redActive)
            return true;

        redOwner = this;
        redActive = true;

        StopRoutine();
        routine = StartCoroutine(
            RedRoutine(Mathf.Max(0.01f, duration), flickerDuration)
        );

        return true;
    }

    /// <summary>
    /// A short pre-transformation flicker that leaves the lights at their
    /// original colours. Used while the cashier goes quiet.
    /// </summary>
    public void Flicker(float duration, float strength = -1f)
    {
        if (redActive || duration <= 0f)
            return;

        Capture();
        StopRoutine();
        routine = StartCoroutine(
            FlickerRoutine(duration, strength < 0f ? flickerDepth : strength)
        );
    }

    public void Restore(float duration)
    {
        if (!captured)
            return;

        StopRoutine();
        redActive = false;

        if (redOwner == this)
            redOwner = null;

        routine = StartCoroutine(
            RestoreRoutine(Mathf.Max(0.01f, duration))
        );
    }

    public void RestoreImmediate()
    {
        StopRoutine();

        if (!captured)
        {
            redActive = false;

            if (redOwner == this)
                redOwner = null;

            return;
        }

        for (int i = 0; i < lightStates.Count; i++)
        {
            LightState state = lightStates[i];

            if (state.light == null)
                continue;

            state.light.color = state.color;
            state.light.intensity = state.intensity;
            state.light.range = state.range;
            state.light.spotAngle = state.spotAngle;
            state.light.innerSpotAngle = state.innerSpotAngle;
            state.light.enabled = state.enabled;
        }

        RestorePanels();

        captured = false;
        redActive = false;

        if (redOwner == this)
            redOwner = null;
    }

    private IEnumerator RedRoutine(float duration, float flickerDuration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float flicker = FlickerValue(elapsed, flickerDuration);

            ApplyRed(t, flicker);
            yield return null;
        }

        ApplyRed(1f, 1f);
        routine = null;
    }

    private IEnumerator FlickerRoutine(float duration, float strength)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float envelope = 1f - Mathf.Clamp01(elapsed / duration);
            float noise = Noise(elapsed);

            ApplyOriginal(1f + noise * strength * envelope);
            ApplyPanelEmission(
                redPanelEmission * Mathf.Max(0f, noise) * envelope * 0.5f
            );

            yield return null;
        }

        ApplyOriginal(1f);
        ApplyPanelEmission(Color.black);
        routine = null;
    }

    private IEnumerator RestoreRoutine(float duration)
    {
        Color[] startColors = new Color[lightStates.Count];
        float[] startIntensities = new float[lightStates.Count];

        for (int i = 0; i < lightStates.Count; i++)
        {
            Light light = lightStates[i].light;

            startColors[i] = light != null ? light.color : lightStates[i].color;
            startIntensities[i] = light != null
                ? light.intensity
                : lightStates[i].intensity;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            for (int i = 0; i < lightStates.Count; i++)
            {
                LightState state = lightStates[i];
                Light light = state.light;

                if (light == null)
                    continue;

                light.color = Color.Lerp(startColors[i], state.color, t);
                light.intensity = Mathf.Lerp(
                    startIntensities[i],
                    state.intensity,
                    t
                );
            }

            ApplyPanelEmission(
                Color.Lerp(redPanelEmission, Color.black, t)
            );

            yield return null;
        }

        RestoreImmediate();
    }

    private void ApplyRed(float amount, float flicker)
    {
        for (int i = 0; i < lightStates.Count; i++)
        {
            LightState state = lightStates[i];
            Light light = state.light;

            if (light == null)
                continue;

            light.color = Color.Lerp(state.color, redLightColor, amount);

            float targetIntensity = state.intensity * Mathf.Lerp(
                1f,
                redIntensityScale,
                amount
            );

            light.intensity = targetIntensity * flicker;
        }

        ApplyPanelEmission(redPanelEmission * amount * flicker);
    }

    private void ApplyOriginal(float intensityScale)
    {
        for (int i = 0; i < lightStates.Count; i++)
        {
            LightState state = lightStates[i];

            if (state.light == null)
                continue;

            state.light.color = state.color;
            state.light.intensity = state.intensity * intensityScale;
        }
    }

    private void ApplyPanelEmission(Color color)
    {
        if (redPanelMaterial == null)
            return;

        redPanelMaterial.SetColor("_EmissionColor", color);
        redPanelMaterial.EnableKeyword("_EMISSION");
    }

    private float FlickerValue(float elapsed, float flickerDuration)
    {
        if (flickerDuration <= 0f || elapsed >= flickerDuration)
            return 1f;

        float envelope = 1f - Mathf.Clamp01(elapsed / flickerDuration);
        float noise = Noise(elapsed);

        return Mathf.Clamp(
            1f + noise * flickerDepth * envelope,
            0.35f,
            1.35f
        );
    }

    private float Noise(float time)
    {
        float perlin = Mathf.PerlinNoise(time * flickerFrequency, 0.37f);
        float pulse = Mathf.Sin(time * flickerFrequency * 3.1f) * 0.25f;

        return (perlin * 2f - 1f) + pulse;
    }

    private void Capture()
    {
        if (captured)
            return;

        CollectIfNeeded();

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];

            if (light == null)
                continue;

            lightStates.Add(new LightState
            {
                light = light,
                color = light.color,
                intensity = light.intensity,
                range = light.range,
                spotAngle = light.spotAngle,
                innerSpotAngle = light.innerSpotAngle,
                enabled = light.enabled
            });
        }

        CapturePanels();
        captured = true;
    }

    private void CollectIfNeeded()
    {
        if (lights == null || lights.Length == 0)
        {
            Light[] found = FindObjectsByType<Light>(
                FindObjectsInactive.Include
            );

            List<Light> selected = new List<Light>();

            for (int i = 0; i < found.Length; i++)
            {
                Light light = found[i];

                if (light == null)
                    continue;

                if (!includeDirectionalLights &&
                    light.type == LightType.Directional)
                {
                    continue;
                }

                selected.Add(light);
            }

            lights = selected.ToArray();
        }

        if (lightPanels != null && lightPanels.Length > 0)
            return;

        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include
        );

        List<Renderer> panels = new List<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || renderer.sharedMaterials == null)
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int m = 0; m < materials.Length; m++)
            {
                if (IsLightPanelMaterial(materials[m]))
                {
                    panels.Add(renderer);
                    break;
                }
            }
        }

        lightPanels = panels.ToArray();
    }

    private bool IsLightPanelMaterial(Material material)
    {
        if (material == null)
            return false;

        if (!material.HasProperty("_EmissionColor"))
            return false;

        if (string.IsNullOrEmpty(lightPanelMaterialName))
            return material.name.ToLowerInvariant().Contains("light");

        return material.name.StartsWith(
            lightPanelMaterialName,
            System.StringComparison.OrdinalIgnoreCase
        );
    }

    private void CapturePanels()
    {
        if (lightPanels == null || lightPanels.Length == 0)
            return;

        Material source = null;

        for (int i = 0; i < lightPanels.Length && source == null; i++)
        {
            Renderer renderer = lightPanels[i];

            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int m = 0; m < materials.Length; m++)
            {
                if (IsLightPanelMaterial(materials[m]))
                {
                    source = materials[m];
                    break;
                }
            }
        }

        if (source == null)
            return;

        redPanelMaterial = new Material(source)
        {
            name = source.name + " (Cashier Red Runtime)"
        };

        redPanelMaterial.EnableKeyword("_EMISSION");
        redPanelMaterial.SetColor("_EmissionColor", Color.black);

        for (int i = 0; i < lightPanels.Length; i++)
        {
            Renderer renderer = lightPanels[i];

            if (renderer == null)
                continue;

            Material[] original = renderer.sharedMaterials;
            Material[] replaced = (Material[])original.Clone();
            List<int> slots = new List<int>();

            for (int m = 0; m < replaced.Length; m++)
            {
                if (!IsLightPanelMaterial(replaced[m]))
                    continue;

                replaced[m] = redPanelMaterial;
                slots.Add(m);
            }

            if (slots.Count == 0)
                continue;

            renderer.sharedMaterials = replaced;

            panelStates.Add(new PanelState
            {
                renderer = renderer,
                originalMaterials = original,
                replacedSlots = slots.ToArray()
            });
        }
    }

    private void RestorePanels()
    {
        for (int i = 0; i < panelStates.Count; i++)
        {
            PanelState state = panelStates[i];

            if (state.renderer != null)
                state.renderer.sharedMaterials = state.originalMaterials;
        }

        panelStates.Clear();

        if (redPanelMaterial != null)
        {
            Destroy(redPanelMaterial);
            redPanelMaterial = null;
        }
    }

    private void StopRoutine()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private void OnDisable()
    {
        RestoreImmediate();
    }

    private void OnDestroy()
    {
        RestoreImmediate();
    }
}
