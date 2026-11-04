using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One place for the project font. The electronic highway-sign font lives in
/// Resources so every runtime UI can load it without a scene reference.
/// </summary>
[NoAutoStaticsCleanup]
public static class GameFont
{
    public const string ResourcePath = "Fonts/ElectronicHighwaySign";
    public const string AssetPath =
        "Assets/Resources/Fonts/ElectronicHighwaySign.ttf";

    private static Font cached;
    private static bool lookedUp;

    public static Font Default
    {
        get
        {
            if (!lookedUp)
            {
                lookedUp = true;
                cached = Resources.Load<Font>(ResourcePath);
            }

#if UNITY_EDITOR
            // Batch-mode and -nographics editors can fail to load dynamic
            // fonts from Resources, so the editor resolves the asset directly.
            if (cached == null)
                cached = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
                    AssetPath
                );
#endif

            if (cached == null)
                cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return cached;
        }
    }

    public static Font Resolve(Font requested)
    {
        return requested != null ? requested : Default;
    }

    /// <summary>
    /// Applies the electronic font to every legacy UI Text and world TextMesh
    /// in the loaded scenes. Runtime-created UI uses TextStyle/GameFont anyway;
    /// this catches prefab HUD text and hand-placed signs.
    /// </summary>
    public static void ApplyToAll()
    {
        Font font = Default;

        if (font == null)
            return;

        Text[] texts = Object.FindObjectsByType<Text>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            if (texts[i].GetComponentInParent<GameFontIgnore>(true) != null)
                continue;

            texts[i].font = font;
        }

        TextMesh[] meshes = Object.FindObjectsByType<TextMesh>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < meshes.Length; i++)
            Apply(meshes[i], font);
    }

    /// <summary>Applies the font to one scene only, for editor tooling.</summary>
    public static void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        Font font = Default;

        if (font == null)
            return;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Text[] texts = roots[i].GetComponentsInChildren<Text>(true);

            for (int j = 0; j < texts.Length; j++)
            {
                if (texts[j] == null)
                    continue;

                if (texts[j].GetComponentInParent<GameFontIgnore>(true) != null)
                    continue;

                texts[j].font = font;
            }

            TextMesh[] meshes =
                roots[i].GetComponentsInChildren<TextMesh>(true);

            for (int j = 0; j < meshes.Length; j++)
                Apply(meshes[j], font);
        }
    }

    private static void Apply(TextMesh mesh, Font font)
    {
        if (mesh == null)
            return;

        mesh.font = font;

        MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();

        if (renderer != null)
            renderer.sharedMaterial = font.material;
    }
}
