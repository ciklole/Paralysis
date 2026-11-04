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
using UnityEngine.UI;

/// <summary>
/// Creates the end-of-demo scene as a normal, editable Canvas hierarchy.
/// The scene contains a camera, an EventSystem, the end Canvas and the
/// controller that animates it. Store.unity and City.unity are never opened,
/// saved or modified by this builder.
/// </summary>
public static class EndScreenBuilder
{
    private const string ScenePath = "Assets/Scenes/End.unity";
    private const string ResourcesFolder = "Assets/Resources/Paralysis";

    private const string MainMenuConfigPath =
        ResourcesFolder + "/MainMenuConfig.asset";

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath) ?? ".";

    [MenuItem("Paralysis/Setup/Build End Screen (safe)")]
    public static void BuildFromMenu()
    {
        Build(false);
    }

    [MenuItem("Paralysis/Setup/Rebuild End Screen")]
    public static void RebuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild End Screen?",
                "This replaces Assets/Scenes/End.unity. Story scenes and " +
                "their assets are not touched.",
                "Rebuild",
                "Cancel"))
        {
            return;
        }

        Build(true);
    }

    /// <summary>Entry point used by the Temp/urp_request.txt dispatcher.</summary>
    public static void BuildBatch()
    {
        Build(false);
    }

    public static void Build(bool overwriteScene)
    {
        StringBuilder report = new StringBuilder();

        try
        {
            MainMenuConfig menu = AssetDatabase.LoadAssetAtPath<MainMenuConfig>(
                MainMenuConfigPath
            );

            if (menu == null)
            {
                menu = ScriptableObject.CreateInstance<MainMenuConfig>();
                report.AppendLine(
                    "WARNING: MainMenuConfig was missing; using defaults."
                );
            }

            if (File.Exists(ScenePath) && !overwriteScene)
            {
                report.AppendLine(
                    "End scene already exists; left untouched."
                );
            }
            else
            {
                CreateScene(menu, report);
            }

            RegisterScene(report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine("Done.");
            Debug.Log("EndScreenBuilder: " + report);
        }
        catch (Exception error)
        {
            report.AppendLine("ERROR: " + error);
            Debug.LogError("EndScreenBuilder: " + error);
        }
        finally
        {
            WriteReport(report.ToString());
        }
    }

    private static void CreateScene(
        MainMenuConfig menu,
        StringBuilder report)
    {
        Scene previousActive = EditorSceneManager.GetActiveScene();
        Scene alreadyOpen = SceneManager.GetSceneByPath(ScenePath);

        if (alreadyOpen.IsValid() && alreadyOpen.isLoaded)
            EditorSceneManager.CloseScene(alreadyOpen, true);

        if (File.Exists(ScenePath))
            AssetDatabase.DeleteAsset(ScenePath);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );

        EditorSceneManager.SetActiveScene(scene);

        CreateCamera(menu);
        AuthoredUIBuilder.EnsureEventSystem(scene);
        CreateCanvas(scene, menu);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        report.AppendLine("Created scene: " + ScenePath);
    }

    private static void CreateCamera(MainMenuConfig menu)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = menu.backgroundColor;
        camera.cullingMask = 0;
        camera.orthographic = true;
        camera.depth = -1;

        if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null)
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateCanvas(Scene scene, MainMenuConfig menu)
    {
        GameObject root = new GameObject(
            "EndCanvas",
            typeof(RectTransform)
        );

        SceneManager.MoveGameObjectToScene(root, scene);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIHelper.ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        MenuAudio audio = root.AddComponent<MenuAudio>();
        audio.Configure(menu.audio);

        EndScreenView view = root.AddComponent<EndScreenView>();
        EndScreenController controller =
            root.AddComponent<EndScreenController>();

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("authoredView").objectReferenceValue = view;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        RectTransform rootRect = root.GetComponent<RectTransform>();

        RectTransform background = UIHelper.CreateStretchRect(
            "Background",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        Image backgroundImage = UIHelper.AddPanel(
            background,
            menu.backgroundColor
        );

        backgroundImage.sprite = menu.backgroundSprite;
        backgroundImage.raycastTarget = true;

        RectTransform content = UIHelper.CreateStretchRect(
            "EndContent",
            rootRect,
            Vector2.zero,
            Vector2.zero
        );

        Text title = CreateTitle(content, menu);

        Text[] lines = CreateLines(content, menu);
        MenuButton backButton = CreateBackButton(content, menu, audio);

        view.canvas = canvas;
        view.backgroundImage = backgroundImage;
        view.titleText = title;
        view.lines = lines;
        view.backButton = backButton;
        view.menuAudio = audio;
        view.applyConfigText = false;
    }

    private static Text CreateTitle(
        Transform parent,
        MainMenuConfig menu)
    {
        TextStyle style = new TextStyle
        {
            font = menu.titleStyle != null ? menu.titleStyle.font : null,
            fontSize = 88,
            bold = true,
            color = Color.white,
            alignment = TextAnchor.MiddleCenter
        };

        RectTransform rect = UIHelper.CreateRect(
            "Title",
            parent,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -170f),
            new Vector2(1500f, 160f)
        );

        Text title = UIHelper.AddText(rect, style, "THE END");

        title.raycastTarget = false;
        title.gameObject.AddComponent<GameFontIgnore>();

        return title;
    }

    private static Text[] CreateLines(
        Transform parent,
        MainMenuConfig menu)
    {
        string[] content =
        {
            "YOU MADE IT OUT.",
            "BUT THE STORE REMEMBERS YOU.",
            "THANK YOU FOR PLAYING THE DEMO."
        };

        TextStyle style = new TextStyle
        {
            font = menu.buttonTextStyle != null
                ? menu.buttonTextStyle.font
                : null,
            fontSize = 34,
            bold = false,
            color = new Color(0.78f, 0.78f, 0.78f, 1f),
            alignment = TextAnchor.MiddleCenter
        };

        Text[] lines = new Text[content.Length];

        for (int i = 0; i < content.Length; i++)
        {
            RectTransform rect = UIHelper.CreateRect(
                "Line" + (i + 1),
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 130f - i * 105f),
                new Vector2(1500f, 70f)
            );

            Text line = UIHelper.AddText(rect, style, content[i]);

            line.raycastTarget = false;
            lines[i] = line;
        }

        return lines;
    }

    private static MenuButton CreateBackButton(
        Transform parent,
        MainMenuConfig menu,
        MenuAudio audio)
    {
        RectTransform rect = UIHelper.CreateRect(
            "BackToMenu",
            parent,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 120f),
            menu.buttonSize
        );

        return AuthoredUIBuilder.AddButton(
            rect,
            "BACK TO MAIN MENU",
            menu.buttonTextStyle,
            menu.buttonStyle,
            audio,
            false,
            string.Empty,
            null,
            Vector2.zero,
            Vector2.zero
        );
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
                ScenePath,
                StringComparison.OrdinalIgnoreCase
            )
        );

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

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

    private static void WriteReport(string report)
    {
        try
        {
            string logs = Path.Combine(ProjectRoot, "Logs");

            if (!Directory.Exists(logs))
                Directory.CreateDirectory(logs);

            File.WriteAllText(
                Path.Combine(logs, "end_build.txt"),
                report
            );
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "EndScreenBuilder: could not write report: " +
                error.Message
            );
        }
    }
}
#endif
