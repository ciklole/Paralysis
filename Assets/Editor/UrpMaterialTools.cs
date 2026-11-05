#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the project on the Universal Render Pipeline. Materials that still
/// use a built-in shader render pink, so this tool reports them and converts
/// them to their URP equivalents.
///
/// The tool can also be triggered from a terminal while the editor is open by
/// writing a command into Temp/urp_request.txt:
///   audit       - write a report of the incompatible materials
///   convert     - convert the incompatible materials of the story scenes
///   pipeline    - make sure URP is the active render pipeline
///   fixdefaults - replace Unity's built-in default material with a URP one
///   buildstore  - rebuild the store scene
///   buildcar    - rebuild the car destination in the driving scene
///   buildmenu   - create the MainMenu scene and menu assets if missing
///   rebuildmenu - replace the MainMenu scene from the builder
///   buildauthoredui - build the editable menu Canvas and UI prefabs
///   importdialogue  - create DialogueSequence assets and the voice report
///   all         - pipeline + fixdefaults + convert + audit
/// </summary>
[NoAutoStaticsCleanup]
public static class UrpMaterialTools
{
    private const string RequestFileName = "urp_request.txt";
    private const string ReportFileName = "urp_material_audit.txt";
    private const string ResultFileName = "urp_material_result.txt";
    private const string SharedFolder = "Assets/Materials/Shared";
    private const string DefaultSurfacePath =
        "Assets/Materials/Shared/DefaultSurface.mat";
    private const string PlayerPrefabPath =
        "Assets/ModularFirstPersonController/FirstPersonController/" +
        "FirstPersonController.prefab";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Home.unity",
        "Assets/Scenes/City.unity",
        "Assets/Scenes/CarDriving.unity",
        "Assets/Scenes/Store.unity"
    };

    private static readonly Dictionary<string, bool> ShaderCache =
        new Dictionary<string, bool>();

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath) ?? ".";

    [InitializeOnLoadMethod]
    private static void OnEditorLoaded()
    {
        EditorApplication.update += PollForRequest;
    }

    private static double nextPollTime;

    /// <summary>
    /// Watches the request file so the tools can be triggered from a
    /// terminal while the editor is open.
    /// </summary>
    private static void PollForRequest()
    {
        if (EditorApplication.timeSinceStartup < nextPollTime)
            return;

        nextPollTime = EditorApplication.timeSinceStartup + 0.5;

        string path = Path.Combine(ProjectRoot, "Temp", RequestFileName);

        if (File.Exists(path))
            RunPendingRequest(path);
    }

    #region Request entry point

    private static void RunPendingRequest(string path)
    {
        string command;

        try
        {
            command = File.ReadAllText(path).Trim();
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "UrpMaterialTools: could not read the request: " + error.Message
            );

            return;
        }

        File.Delete(path);

        // Recovery/diagnostic commands must work while Play Mode is running,
        // because a hung test cannot leave Play Mode on its own.
        if (string.Equals(command, "stopplay", StringComparison.OrdinalIgnoreCase))
        {
            EditorApplication.isPaused = false;
            EditorApplication.isPlaying = false;
            Debug.Log("UrpMaterialTools: requested exit from Play Mode.");
            return;
        }

        if (string.Equals(command, "playstate", StringComparison.OrdinalIgnoreCase))
        {
            WritePlayState();
            return;
        }

        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "UrpMaterialTools: exit Play Mode before running '" +
                command + "'."
            );

            return;
        }

        switch (command)
        {
            case "audit":
                Audit();
                break;

            case "convert":
                Convert();
                break;

            case "pipeline":
                EnsureUrpPipeline();
                break;

            case "fixdefaults":
                FixBuiltInDefaultMaterials();
                break;

            case "buildstore":
                StoreSceneBuilder.BuildStoreScene();
                break;

            case "buildcar":
                CarDriveDestinationBuilder.Build();
                break;

            case "buildmenu":
                MainMenuSceneBuilder.BuildMenuScene();
                break;

            case "rebuildmenu":
                MainMenuSceneBuilder.Build(true);
                break;

            case "buildauthoredui":
                AuthoredUIBuilder.BuildAllBatch();
                break;

            case "buildend":
                EndScreenBuilder.BuildBatch();
                break;

            case "buildcashier":
                CashierSequenceBuilder.Build();
                break;

            case "dumpcheckout":
                CashierSequenceBuilder.DumpCheckout();
                break;

            case "dumpbones":
                CashierSequenceBuilder.DumpBones();
                break;

            case "dumpclips":
                DumpAnimationClips();
                break;

            case "importpumpkin":
                ImportPumpkinClip();
                break;

            case "timedmusic":
                MusicDatabaseEditor.ApplyTimedCueDefaults();
                break;

            case "importdialogue":
                DialogueDatabaseImporter.ImportAll();
                break;

            case "assigndialoguevoice":
                DialogueVoiceAssigner.AssignMissing();
                break;

            case "assigndialoguevoiceoverwrite":
                DialogueVoiceAssigner.AssignAll();
                break;

            case "dumpdriving":
                DrivingSceneSyncTool.DumpHierarchy();
                break;

            case "drivingsyncdry":
                DrivingSceneSyncTool.DryRun();
                break;

            case "drivingsynctest":
                DrivingSceneSyncTool.RunSelfTest();
                break;

            case "smoke":
                StartSmokeTest();
                break;

            case "shot":
                StartScreenshots();
                break;

            case "menushots":
                StartMenuScreenshots();
                break;

            case "newshots":
                StartFeatureScreenshots();
                break;

            case "uishots":
                StartHudScreenshots();
                break;

            case "shotcashier":
                StartCashierScreenshots();
                break;

            case "all":
                EnsureUrpPipeline();
                FixBuiltInDefaultMaterials();
                Convert();
                Audit();
                break;

            default:
                Debug.LogWarning(
                    "UrpMaterialTools: unknown command '" + command + "'."
                );

                break;
        }
    }

    [MenuItem("Paralysis/Setup/Audit URP Materials")]
    public static void Audit()
    {
        StringBuilder report = new StringBuilder();
        Dictionary<string, int> shaderUse =
            new Dictionary<string, int>();
        Dictionary<string, int> offendingUse =
            new Dictionary<string, int>();
        int totalRenderers = 0;

        for (int i = 0; i < ScenePaths.Length; i++)
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePaths[i],
                OpenSceneMode.Additive
            );

            SceneManager.SetActiveScene(scene);

            List<string> sceneOffenders = new List<string>();
            HashSet<string> sceneSeen = new HashSet<string>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int r = 0; r < roots.Length; r++)
            {
                Renderer[] renderers =
                    roots[r].GetComponentsInChildren<Renderer>(true);

                for (int j = 0; j < renderers.Length; j++)
                {
                    Renderer renderer = renderers[j];

                    if (renderer == null)
                        continue;

                    totalRenderers++;

                    Material[] materials = renderer.sharedMaterials;

                    for (int m = 0; m < materials.Length; m++)
                    {
                        Material material = materials[m];

                        if (material == null)
                            continue;

                        string shaderName = material.shader != null
                            ? material.shader.name
                            : "<none>";

                        if (!shaderUse.ContainsKey(shaderName))
                            shaderUse.Add(shaderName, 0);

                        shaderUse[shaderName]++;

                        if (IsUrpCompatible(material))
                            continue;

                        string assetPath = AssetDatabase.GetAssetPath(material);
                        string key = shaderName + "|" + assetPath;

                        if (!offendingUse.ContainsKey(key))
                            offendingUse.Add(key, 0);

                        offendingUse[key]++;

                        if (sceneSeen.Add(key))
                        {
                            sceneOffenders.Add(
                                renderer.name + " -> " + shaderName +
                                " (" + material.name + " @ " + assetPath + ")"
                            );
                        }
                    }
                }
            }

            report.AppendLine(
                "=== " + ScenePaths[i] + " (" + sceneOffenders.Count +
                " incompatible) ==="
            );

            for (int k = 0; k < sceneOffenders.Count; k++)
                report.AppendLine("  " + sceneOffenders[k]);

            EditorSceneManager.CloseScene(scene, true);
        }

        report.AppendLine();
        report.AppendLine("=== shader usage in the story scenes ===");

        List<string> names = new List<string>(shaderUse.Keys);
        names.Sort();

        for (int i = 0; i < names.Count; i++)
        {
            report.AppendLine(
                "  " + shaderUse[names[i]] + "  " + names[i]
            );
        }

        report.AppendLine();
        report.AppendLine("=== incompatible materials ===");

        List<string> badKeys = new List<string>(offendingUse.Keys);
        badKeys.Sort();

        for (int i = 0; i < badKeys.Count; i++)
        {
            report.AppendLine(
                "  " + offendingUse[badKeys[i]] + "  " + badKeys[i]
            );
        }

        report.AppendLine();
        report.AppendLine("renderers checked: " + totalRenderers);
        report.AppendLine("incompatible materials: " + offendingUse.Count);

        Debug.Log("UrpMaterialTools audit:\n" + report);
        WriteFile(ReportFileName, report.ToString());
    }

    [MenuItem("Paralysis/Setup/Convert Materials To URP")]
    public static void Convert()
    {
        StringBuilder report = new StringBuilder();
        HashSet<Material> materials = CollectSceneMaterials();
        int converted = 0;
        int skipped = 0;
        int failed = 0;

        foreach (Material material in materials)
        {
            if (material == null)
                continue;

            if (IsUrpCompatible(material))
            {
                skipped++;
                continue;
            }

            string oldShader = material.shader.name;
            string path = AssetDatabase.GetAssetPath(material);
            string target;

            if (ConvertMaterial(material, out target))
            {
                converted++;
                EditorUtility.SetDirty(material);
                report.AppendLine(
                    "converted " + path + " : " + oldShader + " -> " + target
                );
            }
            else
            {
                failed++;
                report.AppendLine(
                    "FAILED    " + path + " : " + oldShader
                );
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.AppendLine(
            "converted " + converted + ", skipped " + skipped +
            ", failed " + failed
        );

        Debug.Log("UrpMaterialTools convert:\n" + report);
        WriteFile(ResultFileName, report.ToString());
    }

    [MenuItem("Paralysis/Setup/Make URP The Active Pipeline")]
    public static void EnsureUrpPipeline()
    {
        UniversalRenderPipelineAsset asset = null;

        string[] guids = AssetDatabase.FindAssets(
            "t:UniversalRenderPipelineAsset"
        );

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (path.Contains("PC_RPAsset"))
            {
                asset = AssetDatabase.LoadAssetAtPath<
                    UniversalRenderPipelineAsset>(path);

                break;
            }

            UniversalRenderPipelineAsset candidate =
                AssetDatabase.LoadAssetAtPath<
                    UniversalRenderPipelineAsset>(path);

            if (asset == null)
                asset = candidate;
        }

        if (asset == null)
        {
            Debug.LogError(
                "UrpMaterialTools: no UniversalRenderPipelineAsset found in " +
                "the project."
            );

            return;
        }

        GraphicsSettings.defaultRenderPipeline = asset;

        string[] qualityNames = QualitySettings.names;

        for (int i = 0; i < qualityNames.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }

        QualitySettings.SetQualityLevel(1, false);
        AssetDatabase.SaveAssets();

        StringBuilder report = new StringBuilder();
        report.AppendLine("Active URP asset: " + asset.name);
        report.AppendLine("Quality levels: " + string.Join(", ", qualityNames));

        for (int i = 0; i < qualityNames.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            report.AppendLine(
                "  " + qualityNames[i] + " -> " +
                (QualitySettings.renderPipeline != null
                    ? QualitySettings.renderPipeline.name
                    : "<none>")
            );
        }

        QualitySettings.SetQualityLevel(1, false);

        Debug.Log(
            "UrpMaterialTools: URP is now the active render pipeline (" +
            asset.name + ")."
        );

        WriteFile(ResultFileName, report.ToString());
    }

    [MenuItem("Paralysis/Setup/Replace Built-in Default Materials")]
    public static void FixBuiltInDefaultMaterials()
    {
        StringBuilder report = new StringBuilder();
        Material fallback = GetOrCreateDefaultSurface();

        if (fallback == null)
        {
            Debug.LogError(
                "UrpMaterialTools: could not create the shared URP material."
            );

            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            PlayerPrefabPath
        );

        if (prefab != null)
        {
            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(true);

            int changed = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;

                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] == null)
                        continue;

                    if (!IsBuiltInDefault(materials[m]))
                        continue;

                    materials[m] = fallback;
                    changed++;
                }

                if (changed > 0)
                    renderers[i].sharedMaterials = materials;
            }

            if (changed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefabPath);
                report.AppendLine(
                    "player prefab: replaced " + changed + " built-in material(s)"
                );
            }
        }

        // Any scene renderer that still uses a built-in material gets the
        // shared URP material as well.
        for (int i = 0; i < ScenePaths.Length; i++)
        {
            if (!File.Exists(Path.Combine(ProjectRoot, ScenePaths[i])))
                continue;

            Scene scene = EditorSceneManager.OpenScene(
                ScenePaths[i],
                OpenSceneMode.Additive
            );

            SceneManager.SetActiveScene(scene);

            int sceneChanged = 0;
            GameObject[] roots = scene.GetRootGameObjects();

            for (int r = 0; r < roots.Length; r++)
            {
                Renderer[] renderers =
                    roots[r].GetComponentsInChildren<Renderer>(true);

                for (int j = 0; j < renderers.Length; j++)
                {
                    Material[] materials = renderers[j].sharedMaterials;
                    bool dirty = false;

                    for (int m = 0; m < materials.Length; m++)
                    {
                        if (materials[m] == null)
                            continue;

                        if (!IsBuiltInDefault(materials[m]))
                            continue;

                        materials[m] = fallback;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        renderers[j].sharedMaterials = materials;
                        sceneChanged++;
                    }
                }
            }

            if (sceneChanged > 0)
            {
                EditorSceneManager.SaveScene(scene);
                report.AppendLine(
                    ScenePaths[i] + ": fixed " + sceneChanged + " renderer(s)"
                );
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        report.AppendLine("done");
        Debug.Log("UrpMaterialTools fixdefaults:\n" + report);
        WriteFile(ResultFileName, report.ToString());
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Runs the play-mode smoke test inside the open editor. The runtime
    /// test picks up the request file and leaves play mode when it is done.
    /// </summary>
    public static void StartSmokeTest()
    {
        string request = Path.Combine(
            ProjectRoot,
            "Temp",
            "paralysis_smoke.request"
        );

        string results = Path.Combine(
            ProjectRoot,
            "Logs",
            "smoke_results.txt"
        );

        if (File.Exists(results))
            File.Delete(results);

        // Unity removes the project Temp folder on its own, so make sure it
        // is there before the request file is dropped.
        Directory.CreateDirectory(Path.GetDirectoryName(request));
        File.WriteAllText(request, "run");

        EditorApplication.EnterPlaymode();
    }

    /// <summary>
    /// Takes screenshots of the story scenes inside the open editor.
    /// </summary>
    [MenuItem("Paralysis/Setup/Capture Story Scene Screenshots")]
    public static void StartScreenshots()
    {
        string request = Path.Combine(
            ProjectRoot,
            "Temp",
            "paralysis_shot.request"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(request));
        File.WriteAllText(
            request,
            "Store|3|store.png\n" +
            "City|3|city.png\n" +
            "CarDriving|3|car.png\n" +
            "Home|3|home.png\n"
        );

        EditorApplication.EnterPlaymode();
    }

    /// <summary>
    /// Captures the cashier transformation at dialogue, reveal, approach and
    /// reach. Results go to Logs/cashier_*.png and Logs/cashier_probe.txt.
    /// </summary>
    [MenuItem("Paralysis/Setup/Capture Cashier Sequence Screenshots")]
    public static void StartCashierScreenshots()
    {
        string request = Path.Combine(
            ProjectRoot,
            "Temp",
            "paralysis_shot.request"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(request));
        File.WriteAllText(request, "Cashier|3|cashier.png\n");
        EditorApplication.EnterPlaymode();
    }

    /// <summary>
    /// Captures the main menu, pause menu and a chapter intro in the open
    /// editor. Results go to Logs/main_menu.png, Logs/pause_menu.png and
    /// Logs/chapter_intro.png.
    /// </summary>
    [MenuItem("Paralysis/Setup/Capture Menu Screenshots")]
    public static void StartMenuScreenshots()
    {
        string request = Path.Combine(
            ProjectRoot,
            "Temp",
            "paralysis_shot.request"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(request));
        File.WriteAllText(
            request,
            "MainMenu|2|main_menu.png\n" +
            "Pause|3|pause_menu.png\n" +
            "ChapterIntro|2|chapter_intro.png\n"
        );

        EditorApplication.EnterPlaymode();
    }

    /// <summary>
    /// Captures the main menu lightning strike and the third-person driving
    /// camera. Results go to Logs/main_menu.png, Logs/menu_lightning.png
    /// and Logs/car_third_person.png.
    /// </summary>
    [MenuItem("Paralysis/Setup/Capture Feature Screenshots")]
    public static void StartFeatureScreenshots()
    {
        string request = Path.Combine(
            ProjectRoot,
            "Temp",
            "paralysis_shot.request"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(request));
        File.WriteAllText(
            request,
            "MainMenu|3|main_menu.png\n" +
            "MenuLightning|3|menu_lightning.png\n" +
            "CarThirdPerson|4|car_third_person.png\n"
        );

        EditorApplication.EnterPlaymode();
    }

    /// <summary>
    /// Captures the authored dialogue box, objective/instruction HUD and
    /// mission log. Results go to Logs/dialogue.png, Logs/hud_toast.png and
    /// Logs/quest_log.png.
    /// </summary>
    [MenuItem("Paralysis/Setup/Capture HUD Screenshots")]
    public static void StartHudScreenshots()
    {
        string request = Path.Combine(
            ProjectRoot,
            "Temp",
            "paralysis_shot.request"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(request));
        File.WriteAllText(
            request,
            "Dialogue|4|dialogue.png\n" +
            "HudToast|3|hud_toast.png\n" +
            "QuestLog|3|quest_log.png\n"
        );

        EditorApplication.EnterPlaymode();
    }

    private static Material GetOrCreateDefaultSurface()
    {
        if (!AssetDatabase.IsValidFolder(SharedFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Shared");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            DefaultSurfacePath
        );

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                return null;

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, DefaultSurfacePath);
        }

        material.color = new Color(0.16f, 0.16f, 0.18f, 1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();

        return material;
    }

    private static bool IsBuiltInDefault(Material material)
    {
        if (material == null)
            return false;

        string path = AssetDatabase.GetAssetPath(material);

        if (string.IsNullOrEmpty(path))
            return true;

        return path.Contains("unity_builtin_extra") ||
               path.Contains("unity default resources");
    }

    private static HashSet<Material> CollectSceneMaterials()
    {
        HashSet<Material> materials = new HashSet<Material>();

        for (int i = 0; i < ScenePaths.Length; i++)
        {
            if (!File.Exists(Path.Combine(ProjectRoot, ScenePaths[i])))
                continue;

            Scene scene = EditorSceneManager.OpenScene(
                ScenePaths[i],
                OpenSceneMode.Additive
            );

            SceneManager.SetActiveScene(scene);

            GameObject[] roots = scene.GetRootGameObjects();

            for (int r = 0; r < roots.Length; r++)
            {
                Renderer[] renderers =
                    roots[r].GetComponentsInChildren<Renderer>(true);

                for (int j = 0; j < renderers.Length; j++)
                {
                    Material[] slots = renderers[j].sharedMaterials;

                    for (int m = 0; m < slots.Length; m++)
                    {
                        if (slots[m] != null)
                            materials.Add(slots[m]);
                    }
                }
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        return materials;
    }

    /// <summary>
    /// A material renders under URP when it uses a URP shader. Only a few
    /// built-in shaders survive the switch (canvas, sprites and skybox);
    /// everything else - Standard, the legacy shaders and the built-in
    /// particle shaders - shows up pink.
    /// </summary>
    public static bool IsUrpCompatible(Material material)
    {
        if (material == null || material.shader == null)
            return true;

        return IsUrpCompatibleShader(material.shader);
    }

    /// <summary>
    /// True when the shader works under URP.
    /// </summary>
    public static bool IsUrpCompatibleShader(Shader shader)
    {
        if (shader == null)
            return true;

        string name = shader.name;

        if (name.StartsWith("Universal Render Pipeline/"))
            return true;

        // Canvas, sprites and skyboxes keep working with their built-in
        // shaders because Unity renders them outside the SRP passes.
        if (name.StartsWith("UI/"))
            return true;

        if (name.StartsWith("Sprites/"))
            return true;

        if (name.StartsWith("Skybox/"))
            return true;

        string path = AssetDatabase.GetAssetPath(shader);

        if (string.IsNullOrEmpty(path))
            return false;

        bool cached;

        if (ShaderCache.TryGetValue(path, out cached))
            return cached;

        bool result = false;

        try
        {
            string text = File.ReadAllText(path);

            if (path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                result = text.Contains("UniversalPipeline");
            }
            else if (path.EndsWith(
                         ".shadergraph",
                         StringComparison.OrdinalIgnoreCase))
            {
                result = text.Contains(
                    "UnityEditor.Rendering.Universal.ShaderGraph"
                );
            }
        }
        catch (Exception)
        {
            result = false;
        }

        ShaderCache[path] = result;
        return result;
    }

    private static void WritePlayState()
    {
        StringBuilder report = new StringBuilder();

        try
        {
            Scene active = SceneManager.GetActiveScene();

            report.AppendLine("isPlaying: " + EditorApplication.isPlaying);
            report.AppendLine("isPaused: " + EditorApplication.isPaused);
            report.AppendLine(
                "activeScene: " + active.name + " (" + active.path + ")"
            );
            report.AppendLine("timeScale: " + Time.timeScale);
            report.AppendLine("frameCount: " + Time.frameCount);
            report.AppendLine(
                "realtimeSinceStartup: " + Time.realtimeSinceStartup
            );
            report.AppendLine(
                "targetFrameRate: " + Application.targetFrameRate
            );
            report.AppendLine(
                "cursor: " + Cursor.lockState +
                " visible=" + Cursor.visible
            );
            report.AppendLine(
                "UIState.BlocksGameplay: " + UIState.BlocksGameplay
            );
            report.AppendLine(
                "SceneFlow.IsTransitioning: " + SceneFlow.IsTransitioning
            );
            report.AppendLine(
                "SceneFlow.LastError: " + SceneFlow.LastError
            );
            report.AppendLine(
                "PauseManager: " +
                (PauseManager.Instance == null
                    ? "none"
                    : "open=" + PauseManager.Instance.IsOpen +
                      " settings=" + PauseManager.Instance.IsSettingsOpen)
            );
            report.AppendLine(
                "GameProgress: scene=" + GameProgress.CurrentScene +
                " chapter=" + GameProgress.CurrentChapterId +
                " session=" + GameProgress.SessionActive
            );
            report.AppendLine(
                "CarDriveSession.Active: " + CarDriveSession.Active
            );
            report.AppendLine(
                "StoryDirector: " + (StoryDirector.Instance != null)
            );
            report.AppendLine(
                "ObjectiveManager: " + (ObjectiveManager.Instance != null)
            );
            report.AppendLine(
                "PlayerControl.IsLocked: " + PlayerControl.IsLocked
            );

            Transform player = InteractionUtil.FindPlayer();

            report.AppendLine(
                "player: " +
                (player == null ? "none" : player.position.ToString("F2"))
            );
        }
        catch (Exception error)
        {
            report.AppendLine("ERROR: " + error);
        }

        WriteFile("playstate.txt", report.ToString());
    }

    private static void WriteFile(string name, string text)
    {
        string folder = Path.Combine(ProjectRoot, "Logs");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, name);
        File.WriteAllText(path, text);
        Debug.Log("UrpMaterialTools: wrote " + path);
    }

    /// <summary>
    /// The pumpkin monster model ships with a humanoid rig but an empty
    /// animation take, so the model is imported as a human with no clips.
    /// Running this also restores the import settings if they were changed.
    /// </summary>
    private static void ImportPumpkinClip()
    {
        const string path =
            "Assets/Procedural_Materials/pumpkin monster/Base mesh/pumpkin.fbx";

        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

        if (importer == null)
        {
            Debug.LogError(
                "UrpMaterialTools: no model importer for " + path + "."
            );

            return;
        }

        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.clipAnimations = new ModelImporterClipAnimation[0];
        importer.SaveAndReimport();

        Debug.Log(
            "UrpMaterialTools: restored the pumpkin import settings " +
            "(human, no clips)."
        );
    }

    /// <summary>
    /// Lists the animation clips that ship with the character assets so the
    /// procedural drivers know what the asset store already provides.
    /// </summary>
    private static void DumpAnimationClips()
    {
        StringBuilder report = new StringBuilder();

        string[] guids = AssetDatabase.FindAssets("t:Model");

        report.AppendLine("models found: " + guids.Length);

        for (int g = 0; g < guids.Length; g++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[g]);

            if (!path.ToLowerInvariant().Contains("pumpkin") &&
                !path.ToLowerInvariant().Contains("creepy") &&
                !path.ToLowerInvariant().Contains("character") &&
                !path.ToLowerInvariant().Contains("npc"))
            {
                continue;
            }

            report.AppendLine("=== " + path + " ===");

            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(path);

            report.AppendLine("  sub assets: " + assets.Length);

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] == null)
                    continue;

                report.AppendLine(
                    "  asset: " + assets[i].name +
                    " | type=" + assets[i].GetType().FullName
                );

                AnimationClip clip = assets[i] as AnimationClip;

                if (clip == null)
                    continue;

                AnimationClipSettings settings =
                    AnimationUtility.GetAnimationClipSettings(clip);

                report.AppendLine(
                    "clip: " + clip.name +
                    " | length=" + clip.length.ToString("F3") +
                    " | frameRate=" + clip.frameRate.ToString("F1") +
                    " | humanMotion=" + clip.humanMotion +
                    " | legacy=" + clip.legacy +
                    " | loop=" + settings.loopTime +
                    " | cycleOffset=" + settings.cycleOffset.ToString("F2")
                );

                EditorCurveBinding[] bindings =
                    AnimationUtility.GetCurveBindings(clip);

                report.AppendLine("  bindings: " + bindings.Length);

                for (int b = 0; b < bindings.Length; b++)
                {
                    EditorCurveBinding binding = bindings[b];

                    report.AppendLine(
                        "    " + binding.path + " | " +
                        binding.propertyName + " | " +
                        binding.type
                    );
                }
            }
        }

        WriteFile("animation_clips.txt", report.ToString());
    }

    #endregion

    #region Conversion

    private static bool ConvertMaterial(Material material, out string target)
    {
        target = string.Empty;

        string name = material.shader.name;

        if (name == "Standard" || name == "Standard (Specular setup)")
        {
            target = "Universal Render Pipeline/Lit";

            if (!ApplyShader(material, target))
                return false;

            CopyStandardProperties(material);
            return true;
        }

        if (name.StartsWith("Legacy Shaders/Diffuse") ||
            name.StartsWith("Legacy Shaders/Transparent/Diffuse") ||
            name == "Diffuse")
        {
            target = "Universal Render Pipeline/Lit";

            if (!ApplyShader(material, target))
                return false;

            CopyStandardProperties(material);

            if (name.Contains("Transparent"))
                ApplyTransparent(material);
            else
                ApplyOpaque(material);

            return true;
        }

        if (name == "Unlit/Color" || name == "Unlit/Texture" ||
            name.StartsWith("Legacy Shaders/Unlit"))
        {
            target = "Universal Render Pipeline/Unlit";

            if (!ApplyShader(material, target))
                return false;

            CopyUnlitProperties(material);
            return true;
        }

        if (name == "Sprites/Default")
        {
            target = "Universal Render Pipeline/Unlit";

            if (!ApplyShader(material, target))
                return false;

            CopyUnlitProperties(material);
            ApplyTransparent(material);
            return true;
        }

        if (name.StartsWith("Particles/") ||
            name.StartsWith("Legacy Shaders/Particles/"))
        {
            target = "Universal Render Pipeline/Particles/Particles Unlit";

            if (!ApplyShader(material, target))
                return false;

            CopyUnlitProperties(material);
            ApplyTransparent(material);
            return true;
        }

        return false;
    }

    private static bool ApplyShader(Material material, string shaderName)
    {
        Shader shader = Shader.Find(shaderName);

        if (shader == null)
        {
            Debug.LogError(
                "UrpMaterialTools: shader '" + shaderName + "' was not found."
            );

            return false;
        }

        material.shader = shader;
        return true;
    }

    private static void CopyStandardProperties(Material material)
    {
        CopyTexture(material, "_MainTex", "_BaseMap");
        CopyTexture(material, "_BumpMap", "_BumpMap");
        CopyTexture(material, "_ParallaxMap", "_ParallaxMap");
        CopyTexture(material, "_MetallicGlossMap", "_MetallicGlossMap");
        CopyTexture(material, "_OcclusionMap", "_OcclusionMap");
        CopyTexture(material, "_EmissionMap", "_EmissionMap");
        CopyTexture(material, "_SpecGlossMap", "_MetallicGlossMap");
        CopyTexture(material, "_DetailAlbedoMap", "_DetailAlbedoMap");

        CopyColor(material, "_Color", "_BaseColor");
        CopyColor(material, "_EmissionColor", "_EmissionColor");
        CopyColor(material, "_SpecColor", "_BaseColor");

        CopyFloat(material, "_Metallic", "_Metallic");
        CopyFloat(material, "_Glossiness", "_Smoothness");
        CopyFloat(material, "_GlossMapScale", "_Smoothness");
        CopyFloat(material, "_Cutoff", "_Cutoff");
        CopyFloat(material, "_BumpScale", "_BumpScale");
        CopyFloat(material, "_Parallax", "_Parallax");

        if (material.GetTexture("_BumpMap") != null)
            material.EnableKeyword("_NORMALMAP");

        if (material.GetTexture("_MetallicGlossMap") != null)
            material.EnableKeyword("_METALLICSPECGLOSSMAP");

        if (material.GetTexture("_OcclusionMap") != null)
            material.EnableKeyword("_OCCLUSIONMAP");

        ApplyOpaque(material);
    }

    private static void CopyUnlitProperties(Material material)
    {
        CopyTexture(material, "_MainTex", "_BaseMap");
        CopyColor(material, "_Color", "_BaseColor");
        CopyFloat(material, "_Cutoff", "_Cutoff");
    }

    private static void ApplyOpaque(Material material)
    {
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", 1f);
        material.SetFloat("_DstBlend", 0f);
        material.SetFloat("_ZWrite", 1f);
        material.SetFloat("_AlphaClip", 0f);
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = -1;
    }

    private static void ApplyTransparent(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", 5f);
        material.SetFloat("_DstBlend", 10f);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
    }

    private static void CopyTexture(
        Material material,
        string from,
        string to)
    {
        if (!material.HasProperty(from) || !material.HasProperty(to))
            return;

        Texture texture = material.GetTexture(from);

        if (texture == null)
            return;

        material.SetTexture(to, texture);
        material.SetTextureScale(to, material.GetTextureScale(from));
        material.SetTextureOffset(to, material.GetTextureOffset(from));
    }

    private static void CopyColor(
        Material material,
        string from,
        string to)
    {
        if (!material.HasProperty(from) || !material.HasProperty(to))
            return;

        material.SetColor(to, material.GetColor(from));
    }

    private static void CopyFloat(
        Material material,
        string from,
        string to)
    {
        if (!material.HasProperty(from) || !material.HasProperty(to))
            return;

        material.SetFloat(to, material.GetFloat(from));
    }

    #endregion
}
#endif
