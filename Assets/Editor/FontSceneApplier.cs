#if UNITY_EDITOR
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the project's electronic font to hand-placed legacy Text and
/// TextMesh components in the story scenes, then saves those scenes.
/// </summary>
[NoAutoStaticsCleanup]
public static class FontSceneApplier
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Home.unity",
        "Assets/Scenes/City.unity",
        "Assets/Scenes/CarDriving.unity",
        "Assets/Scenes/Store.unity"
    };


    [MenuItem("Paralysis/Setup/Apply Electronic Font To Story Scenes")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "FontSceneApplier: exit Play Mode before applying fonts."
            );

            return;
        }

        for (int i = 0; i < ScenePaths.Length; i++)
        {
            ApplyToScene(ScenePaths[i]);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("FontSceneApplier: applied the electronic font to story scenes.");
    }

    private static void ApplyToScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        GameFont.ApplyToScene(scene);

        int missing = 0;
        int applied = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            UnityEngine.UI.Text[] texts =
                roots[i].GetComponentsInChildren<UnityEngine.UI.Text>(true);

            for (int j = 0; j < texts.Length; j++)
            {
                if (texts[j] == null)
                    continue;

                if (texts[j].font == GameFont.Default)
                    applied++;
                else
                    missing++;
            }

            TextMesh[] meshes =
                roots[i].GetComponentsInChildren<TextMesh>(true);

            for (int j = 0; j < meshes.Length; j++)
            {
                if (meshes[j] == null)
                    continue;

                if (meshes[j].font == GameFont.Default)
                    applied++;
                else
                    missing++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);

        Debug.Log(
            "FontSceneApplier: " + path + " applied=" + applied +
            " missing=" + missing
        );
    }
}
#endif
