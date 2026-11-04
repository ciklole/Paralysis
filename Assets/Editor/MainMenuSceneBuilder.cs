#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the MainMenu scene and the Resources assets that drive the menus.
/// The builder is additive, so running it never saves, replaces or dirties the
/// user's currently open Store/City scene.
/// </summary>
public static class MainMenuSceneBuilder
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string ResourcesFolder = "Assets/Resources/Paralysis";

    private const string SettingsConfigPath =
        ResourcesFolder + "/SettingsConfig.asset";

    private const string ChapterDatabasePath =
        ResourcesFolder + "/ChapterDatabase.asset";

    private const string SceneFlowConfigPath =
        ResourcesFolder + "/SceneFlowConfig.asset";

    private const string PauseMenuConfigPath =
        ResourcesFolder + "/PauseMenuConfig.asset";

    private const string MainMenuConfigPath =
        ResourcesFolder + "/MainMenuConfig.asset";

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath) ?? ".";

    [MenuItem("Paralysis/Setup/Build Main Menu (safe)")]
    public static void BuildMenuScene()
    {
        Build(false);
    }

    [MenuItem("Paralysis/Setup/Rebuild Main Menu Scene")]
    public static void RebuildMenuScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild Main Menu?",
                "This replaces Assets/Scenes/MainMenu.unity. Story scenes and " +
                "their assets are not touched.",
                "Rebuild",
                "Cancel"))
        {
            return;
        }

        Build(true);
    }

    public static void Build(bool overwriteScene)
    {
        StringBuilder report = new StringBuilder();

        try
        {
            EnsureFolder("Assets/Resources", "Paralysis");

            SettingsConfig settings = LoadOrCreate<SettingsConfig>(
                SettingsConfigPath
            );

            ChapterDatabase chapters = LoadOrCreate<ChapterDatabase>(
                ChapterDatabasePath
            );

            if (chapters.chapters == null || chapters.chapters.Count == 0)
                PopulateChapters(chapters);

            ChapterDatabase.ClearCache();

            SceneFlowConfig flow = LoadOrCreate<SceneFlowConfig>(
                SceneFlowConfigPath
            );

            flow.mainMenuScene = "MainMenu";

            PauseMenuConfig pause = LoadOrCreate<PauseMenuConfig>(
                PauseMenuConfigPath
            );

            MainMenuConfig menu = LoadOrCreate<MainMenuConfig>(
                MainMenuConfigPath
            );

            AssignTitleFont(menu);

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(chapters);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(pause);
            EditorUtility.SetDirty(menu);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine("SettingsConfig: " + SettingsConfigPath);
            report.AppendLine("ChapterDatabase: " + ChapterDatabasePath);
            report.AppendLine("SceneFlowConfig: " + SceneFlowConfigPath);
            report.AppendLine("PauseMenuConfig: " + PauseMenuConfigPath);
            report.AppendLine("MainMenuConfig: " + MainMenuConfigPath);

            if (File.Exists(MainMenuScenePath) && !overwriteScene)
            {
                report.AppendLine(
                    "MainMenu scene already exists; left untouched."
                );
            }
            else
            {
                CreateScene(menu, chapters, report);
            }

            RegisterScene(report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine("Done.");
            Debug.Log("MainMenuSceneBuilder: " + report);
        }
        catch (Exception error)
        {
            report.AppendLine("ERROR: " + error);
            Debug.LogError("MainMenuSceneBuilder: " + error);
        }
        finally
        {
            WriteReport(report.ToString());
        }
    }

    private static void CreateScene(
        MainMenuConfig menu,
        ChapterDatabase chapters,
        StringBuilder report)
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();

        Scene alreadyOpen = SceneManager.GetSceneByPath(MainMenuScenePath);

        if (alreadyOpen.IsValid() && alreadyOpen.isLoaded)
            EditorSceneManager.CloseScene(alreadyOpen, true);

        if (File.Exists(MainMenuScenePath))
            AssetDatabase.DeleteAsset(MainMenuScenePath);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(scene);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.orthographic = true;
        camera.depth = -1;

        if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null)
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

        cameraObject.AddComponent<AudioListener>();

        GameObject controllerObject = new GameObject("MainMenu");
        MainMenuController controller =
            controllerObject.AddComponent<MainMenuController>();

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("config").objectReferenceValue = menu;
        serialized.FindProperty("chapterDatabase").objectReferenceValue =
            chapters;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        AuthoredUIBuilder.BuildMainMenuCanvas(
            scene,
            controller,
            menu,
            chapters
        );

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        EditorSceneManager.CloseScene(scene, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        report.AppendLine("Created scene: " + MainMenuScenePath);
    }

    private static void RegisterScene(StringBuilder report)
    {
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes
            );

        scenes.RemoveAll(scene =>
            string.Equals(
                scene.path,
                MainMenuScenePath,
                StringComparison.OrdinalIgnoreCase
            )
        );

        scenes.Insert(
            0,
            new EditorBuildSettingsScene(MainMenuScenePath, true)
        );

        EditorBuildSettings.scenes = scenes.ToArray();

        report.AppendLine("Build Settings order:");

        for (int i = 0; i < scenes.Count; i++)
        {
            report.AppendLine(
                "  [" + i + "] " + scenes[i].path +
                (scenes[i].enabled ? string.Empty : " (disabled)")
            );
        }
    }

    private static void PopulateChapters(ChapterDatabase database)
    {
        database.availabilityMode =
            ChapterAvailabilityMode.Development_AllChapters;

        database.chapters = new List<ChapterDefinition>
        {
            new ChapterDefinition
            {
                chapterId = "chapter_1_apartment",
                chapterNumber = "1",
                chapterTitle = "THE APARTMENT",
                subtitle = "SOMETHING IS WRONG",
                description =
                    "Wake up, turn off the lights and leave the apartment.",
                sceneName = "Home",
                displayOrder = 0,
                enabled = true,
                showChapterIntro = true,
                useGlobalIntroStyle = true
            },
            new ChapterDefinition
            {
                chapterId = "chapter_2_city",
                chapterNumber = "2",
                chapterTitle = "THE CITY",
                subtitle = "KEEP MOVING",
                description =
                    "Drive through the empty city and find the store.",
                sceneName = "City",
                displayOrder = 1,
                enabled = true,
                showChapterIntro = true,
                useGlobalIntroStyle = true
            },
            new ChapterDefinition
            {
                chapterId = "chapter_3_store",
                chapterNumber = "3",
                chapterTitle = "THE STORE",
                subtitle = "CLOSING TIME",
                description =
                    "Collect what you need before the cashier notices you.",
                sceneName = "Store",
                displayOrder = 2,
                enabled = true,
                showChapterIntro = true,
                useGlobalIntroStyle = true
            }
        };
    }

    private static void AssignTitleFont(MainMenuConfig menu)
    {
        if (menu.titleStyle == null)
            menu.titleStyle = new TextStyle();

        if (menu.titleStyle.font != null)
            return;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/Resources/Fonts/PlaceholderTitle.ttf"
        );

        if (font == null)
            font = Resources.Load<Font>("Fonts/PlaceholderTitle");

        if (font == null)
            font = GameFont.Default;

        menu.titleStyle.font = font;
    }

    private static T LoadOrCreate<T>(string path)
        where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);

        return asset;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (AssetDatabase.IsValidFolder(parent + "/" + child))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder("Assets", parent.Substring("Assets/".Length));

        AssetDatabase.CreateFolder(parent, child);
    }

    private static void WriteReport(string report)
    {
        try
        {
            string logs = Path.Combine(ProjectRoot, "Logs");

            if (!Directory.Exists(logs))
                Directory.CreateDirectory(logs);

            File.WriteAllText(
                Path.Combine(logs, "mainmenu_build.txt"),
                report
            );
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "MainMenuSceneBuilder: could not write report: " +
                error.Message
            );
        }
    }
}
#endif
