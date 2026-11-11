#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Copies the environment between the City and CarDriving scenes.
///
/// The forward direction (City -> CarDriving) copies the authored City
/// environment into the driving scene without touching the driving-only
/// objects (car, walking player, camera, destination and the CarDriveSystems
/// root). The reverse direction (CarDriving -> City) copies the driving
/// scene's environment, lighting and fog back into City while preserving
/// City's player and story managers.
///
/// The sync uses EditorSceneManager.MergeScenes instead of recreating objects
/// by hand. That keeps prefab instances, overrides, materials and internal
/// references exactly as they are in the source scene. The source scene is
/// opened additively and is never saved, so it is not modified.
/// </summary>
public static class DrivingSceneSyncTool
{
    private const string CityPath = "Assets/Scenes/City.unity";
    private const string DrivingPath = "Assets/Scenes/CarDriving.unity";
    private const string DumpFileName = "scene_structure.txt";
    private const string ReportFileName = "driving_sync.txt";
    private const string SelfTestFileName = "driving_sync_selftest.txt";
    private const string SelfTestFolder = "Assets/__DrivingSyncTest";

    /// <summary>
    /// Objects that belong to the driving experience, not to the City
    /// environment. They are never replaced or deleted by the sync.
    /// </summary>
    private static readonly string[] ProtectedRootNames =
    {
        "CarDriveSystems",
        "Store Destination",
        "StoryManagers",
        "FirstPersonController",
        "Free Racing Car"
    };

    private static readonly string[] ProtectedNameFragments =
    {
        "Player",
        "CarCamera",
        "CarDrive"
    };

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath) ?? ".";

    private static string LogPath(string fileName)
    {
        return Path.Combine(ProjectRoot, "Logs", fileName);
    }

    [MenuItem("Paralysis/Scenes/Driving Sync/Dry Run (report only)")]
    public static void DryRun()
    {
        Sync(false, CityPath, DrivingPath, false, true);
    }

    [MenuItem("Paralysis/Scenes/Driving Sync/Apply City -> CarDriving")]
    public static void ApplySync()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Driving Sync",
                "Exit Play Mode before syncing scenes.",
                "OK"
            );

            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply City -> CarDriving",
                "This replaces the environment objects in CarDriving with the " +
                "current City environment. The car, player, camera, destination " +
                "and story managers are preserved.\n\n" +
                "City.unity itself is not modified. Continue?",
                "Apply",
                "Cancel"))
        {
            return;
        }

        Sync(true, CityPath, DrivingPath, true, false);
    }

    [MenuItem("Paralysis/Scenes/Driving Sync/Apply CarDriving -> City")]
    public static void ApplyReverseSync()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Driving Sync",
                "Exit Play Mode before syncing scenes.",
                "OK"
            );

            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply CarDriving -> City",
                "This replaces the environment objects in City with the " +
                "current CarDriving environment, including the lighting and " +
                "fog settings. The City player and story managers are " +
                "preserved.\n\n" +
                "CarDriving.unity itself is not modified. Continue?",
                "Apply",
                "Cancel"))
        {
            return;
        }

        Sync(true, DrivingPath, CityPath, true, false, true);
    }

    /// <summary>Batch-mode entry point for the forward sync.</summary>
    public static void ApplySyncBatch()
    {
        bool applied = Sync(true, CityPath, DrivingPath, false, true);
        Debug.Log("DrivingSceneSyncTool: forward sync applied=" + applied);
    }

    /// <summary>Batch-mode entry point for the reverse sync.</summary>
    public static void ApplyReverseSyncBatch()
    {
        bool applied = Sync(true, DrivingPath, CityPath, false, true, true);
        Debug.Log("DrivingSceneSyncTool: reverse sync applied=" + applied);
    }

    [MenuItem("Paralysis/Scenes/Dump City and CarDriving Hierarchy")]
    public static void DumpHierarchy()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "DrivingSceneSyncTool: exit Play Mode before dumping scenes."
            );

            return;
        }

        Scene original = SceneManager.GetActiveScene();
        Scene city = OpenAdditive(CityPath);
        Scene driving = OpenAdditive(DrivingPath);

        StringBuilder report = new StringBuilder();

        report.AppendLine("City and CarDriving hierarchy");
        report.AppendLine("Generated " + DateTime.Now.ToString("u"));
        report.AppendLine();

        if (city.IsValid())
            AppendScene(report, city);
        else
            report.AppendLine("Could not open " + CityPath);

        report.AppendLine();

        if (driving.IsValid())
            AppendScene(report, driving);
        else
            report.AppendLine("Could not open " + DrivingPath);

        WriteReport(report, DumpFileName);

        if (driving.IsValid() && driving != original)
            EditorSceneManager.CloseScene(driving, true);

        if (city.IsValid() && city != original)
            EditorSceneManager.CloseScene(city, true);

        if (original.IsValid() && original.isLoaded)
            SceneManager.SetActiveScene(original);
    }

    /// <summary>
    /// Runs the sync against a pair of scenes. When apply is false, it only
    /// reports the differences. Returns true when the operation completed.
    /// </summary>
    public static bool Sync(
        bool apply,
        string cityPath,
        string drivingPath,
        bool activateTarget,
        bool closeTarget,
        bool copySceneSettings = false)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "DrivingSceneSyncTool: exit Play Mode before syncing scenes."
            );

            return false;
        }

        if (string.IsNullOrEmpty(cityPath) ||
            string.IsNullOrEmpty(drivingPath) ||
            string.Equals(cityPath, drivingPath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                "DrivingSceneSyncTool: City and CarDriving paths must differ."
            );

            return false;
        }

        if (!File.Exists(cityPath) || !File.Exists(drivingPath))
        {
            Debug.LogError(
                "DrivingSceneSyncTool: could not find both scene files."
            );

            return false;
        }

        Scene original = SceneManager.GetActiveScene();
        bool cityWasLoaded = IsLoaded(cityPath);
        bool drivingWasLoaded = IsLoaded(drivingPath);
        Scene city = OpenAdditive(cityPath);
        Scene driving = OpenAdditive(drivingPath);

        if (!city.IsValid() || !driving.IsValid())
        {
            Debug.LogError(
                "DrivingSceneSyncTool: could not open both scenes."
            );

            CloseIfOpened(city, cityWasLoaded);
            CloseIfOpened(driving, drivingWasLoaded);
            RestoreActiveScene(original);
            return false;
        }

        if (apply && driving.isDirty)
        {
            Debug.LogError(
                "DrivingSceneSyncTool: save " + drivingPath +
                " before applying the sync."
            );

            CloseIfOpened(city, cityWasLoaded);
            CloseIfOpened(driving, drivingWasLoaded);
            RestoreActiveScene(original);
            return false;
        }

        List<GameObject> protectedRoots = CollectProtectedRoots(driving);
        List<GameObject> environmentRoots = CollectEnvironmentRoots(
            driving,
            protectedRoots
        );
        GameObject[] sourceRoots = city.GetRootGameObjects();
        List<GameObject> sourceEnvironment = new List<GameObject>();

        for (int i = 0; i < sourceRoots.Length; i++)
        {
            if (sourceRoots[i] != null && !IsProtectedRoot(sourceRoots[i]))
                sourceEnvironment.Add(sourceRoots[i]);
        }

        StringBuilder report = new StringBuilder();

        report.AppendLine(copySceneSettings
            ? "CarDriving -> City environment sync"
            : "City -> CarDriving environment sync");
        report.AppendLine("Generated " + DateTime.Now.ToString("u"));
        report.AppendLine("Mode: " + (apply ? "APPLY" : "DRY RUN"));
        report.AppendLine("City: " + cityPath);
        report.AppendLine("Driving: " + drivingPath);
        report.AppendLine();

        report.AppendLine(copySceneSettings
            ? "Preserved City roots:"
            : "Preserved driving roots:");
        AppendNames(report, protectedRoots, "  ");
        report.AppendLine();

        report.AppendLine(copySceneSettings
            ? "CarDriving environment roots:"
            : "City environment roots:");
        AppendNames(report, sourceEnvironment, "  ");
        report.AppendLine();

        report.AppendLine(copySceneSettings
            ? "City environment roots (will be replaced):"
            : "CarDriving environment roots (will be replaced):");
        AppendNames(report, environmentRoots, "  ");
        report.AppendLine();

        AppendEnvironmentDiff(report, sourceEnvironment, environmentRoots);

        if (!apply)
        {
            WriteReport(report, ReportFileName);

            CloseIfOpened(city, cityWasLoaded);
            CloseIfOpened(driving, drivingWasLoaded);
            RestoreActiveScene(original);

            return true;
        }

        // The source scene is authoritative for both directions. Copy the
        // RenderSettings so lighting, skybox and fog always travel with the
        // environment, even when the object lists already match.
        CopyRenderSettings(city, driving);

        // Remove the old environment. Driving-only roots are never touched.
        for (int i = 0; i < environmentRoots.Count; i++)
        {
            if (environmentRoots[i] != null)
                UnityEngine.Object.DestroyImmediate(environmentRoots[i]);
        }

        // Move every City root into CarDriving. This preserves prefab links,
        // overrides and references without manually copying serialized data.
        EditorSceneManager.MergeScenes(city, driving);

        // City also contains its own walking player and story managers. Those
        // duplicates must not replace the ones the driving scene already owns.
        for (int i = 0; i < sourceRoots.Length; i++)
        {
            GameObject sourceRoot = sourceRoots[i];

            if (sourceRoot != null && IsProtectedRoot(sourceRoot))
                UnityEngine.Object.DestroyImmediate(sourceRoot);
        }

        EditorSceneManager.MarkSceneDirty(driving);
        bool saved = EditorSceneManager.SaveScene(driving);

        report.AppendLine();
        report.AppendLine(
            "Result: " + (saved ? "saved" : "SAVE FAILED")
        );

        WriteReport(report, ReportFileName);

        if (closeTarget)
            CloseIfOpened(driving, drivingWasLoaded);
        else if (activateTarget && driving.IsValid() && driving.isLoaded)
            SceneManager.SetActiveScene(driving);
        else
            RestoreActiveScene(original);

        return saved;
    }

    /// <summary>
    /// Creates copies of the real scenes, applies the sync to the copies and
    /// validates the result. The copies are deleted afterwards, so the user's
    /// City and CarDriving scenes are never touched by the self-test.
    /// </summary>
    public static void RunSelfTest()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "DrivingSceneSyncTool: exit Play Mode before running the test."
            );

            return;
        }

        StringBuilder report = new StringBuilder();

        report.AppendLine("City -> CarDriving sync self-test");
        report.AppendLine("Generated " + DateTime.Now.ToString("u"));
        report.AppendLine();

        if (AssetDatabase.IsValidFolder(SelfTestFolder))
            AssetDatabase.DeleteAsset(SelfTestFolder);

        AssetDatabase.CreateFolder("Assets", "__DrivingSyncTest");

        string testCity = SelfTestFolder + "/City.unity";
        string testDriving = SelfTestFolder + "/CarDriving.unity";

        if (!AssetDatabase.CopyAsset(CityPath, testCity) ||
            !AssetDatabase.CopyAsset(DrivingPath, testDriving))
        {
            report.AppendLine("FAIL  could not copy the scenes");
            WriteReport(report, SelfTestFileName);
            AssetDatabase.DeleteAsset(SelfTestFolder);
            return;
        }

        AssetDatabase.SaveAssets();

        bool applied = Sync(
            true,
            testCity,
            testDriving,
            false,
            true
        );

        report.AppendLine(
            (applied ? "PASS" : "FAIL") + "  sync applies to a copy"
        );

        if (applied)
        {
            Scene copy = EditorSceneManager.OpenScene(
                testDriving,
                OpenSceneMode.Additive
            );

            GameObject[] roots = copy.GetRootGameObjects();
            Dictionary<string, int> counts = new Dictionary<string, int>();

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                    continue;

                int count;

                counts.TryGetValue(roots[i].name, out count);
                counts[roots[i].name] = count + 1;
            }

            bool protectedOk = true;

            for (int i = 0; i < ProtectedRootNames.Length; i++)
            {
                string name = ProtectedRootNames[i];
                int count;

                counts.TryGetValue(name, out count);

                if (count != 1)
                    protectedOk = false;

                report.AppendLine(
                    "  " + name + ": " + count +
                    (count == 1 ? " (ok)" : " (FAIL)")
                );
            }

            report.AppendLine(
                (protectedOk ? "PASS" : "FAIL") +
                "  each driving root exists exactly once"
            );

            Scene cityCopy = EditorSceneManager.OpenScene(
                testCity,
                OpenSceneMode.Additive
            );
            GameObject[] cityRoots = cityCopy.GetRootGameObjects();
            int expectedEnvironment = 0;

            for (int i = 0; i < cityRoots.Length; i++)
            {
                if (cityRoots[i] != null && !IsProtectedRoot(cityRoots[i]))
                    expectedEnvironment++;
            }

            int actualEnvironment = 0;

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && !IsProtectedRoot(roots[i]))
                    actualEnvironment++;
            }

            report.AppendLine(
                (expectedEnvironment == actualEnvironment ? "PASS" : "FAIL") +
                "  environment root count is " + actualEnvironment +
                " (expected " + expectedEnvironment + ")"
            );

            if (cityCopy.IsValid())
                EditorSceneManager.CloseScene(cityCopy, true);

            if (copy.IsValid())
                EditorSceneManager.CloseScene(copy, true);
        }

        WriteReport(report, SelfTestFileName);
        AssetDatabase.DeleteAsset(SelfTestFolder);
        AssetDatabase.Refresh();
    }

    private static bool IsLoaded(string path)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        return scene.IsValid() && scene.isLoaded;
    }

    private static Scene OpenAdditive(string path)
    {
        Scene existing = SceneManager.GetSceneByPath(path);

        if (existing.IsValid() && existing.isLoaded)
            return existing;

        return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }

    private static void CloseIfOpened(Scene scene, bool wasLoaded)
    {
        if (!wasLoaded && scene.IsValid() && scene.isLoaded)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void RestoreActiveScene(Scene original)
    {
        if (original.IsValid() && original.isLoaded)
            SceneManager.SetActiveScene(original);
    }

    /// <summary>
    /// Copies the per-scene render settings (fog, ambient light, skybox and
    /// reflection settings) from one loaded scene to another. RenderSettings
    /// belongs to the active scene, so the active scene is switched while the
    /// values are read and written.
    /// </summary>
    private static void CopyRenderSettings(Scene source, Scene target)
    {
        Scene previous = SceneManager.GetActiveScene();

        SceneManager.SetActiveScene(source);

        bool fog = RenderSettings.fog;
        Color fogColor = RenderSettings.fogColor;
        FogMode fogMode = RenderSettings.fogMode;
        float fogDensity = RenderSettings.fogDensity;
        float fogStart = RenderSettings.fogStartDistance;
        float fogEnd = RenderSettings.fogEndDistance;
        AmbientMode ambientMode = RenderSettings.ambientMode;
        Color ambientSky = RenderSettings.ambientSkyColor;
        Color ambientEquator = RenderSettings.ambientEquatorColor;
        Color ambientGround = RenderSettings.ambientGroundColor;
        float ambientIntensity = RenderSettings.ambientIntensity;
        Color subtractiveShadow = RenderSettings.subtractiveShadowColor;
        Material skybox = RenderSettings.skybox;
        Light sun = RenderSettings.sun;
        float haloStrength = RenderSettings.haloStrength;
        float flareStrength = RenderSettings.flareStrength;
        float flareFadeSpeed = RenderSettings.flareFadeSpeed;
        DefaultReflectionMode reflectionMode = RenderSettings.defaultReflectionMode;
        int reflectionResolution = RenderSettings.defaultReflectionResolution;
        int reflectionBounces = RenderSettings.reflectionBounces;
        float reflectionIntensity = RenderSettings.reflectionIntensity;

        SceneManager.SetActiveScene(target);

        RenderSettings.fog = fog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.subtractiveShadowColor = subtractiveShadow;
        RenderSettings.skybox = skybox;
        RenderSettings.sun = sun;
        RenderSettings.haloStrength = haloStrength;
        RenderSettings.flareStrength = flareStrength;
        RenderSettings.flareFadeSpeed = flareFadeSpeed;
        RenderSettings.defaultReflectionMode = reflectionMode;
        RenderSettings.defaultReflectionResolution = reflectionResolution;
        RenderSettings.reflectionBounces = reflectionBounces;
        RenderSettings.reflectionIntensity = reflectionIntensity;

        if (previous.IsValid() && previous.isLoaded)
            SceneManager.SetActiveScene(previous);
    }

    private static List<GameObject> CollectProtectedRoots(Scene scene)
    {
        List<GameObject> result = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && IsProtectedRoot(roots[i]))
                result.Add(roots[i]);
        }

        return result;
    }

    private static List<GameObject> CollectEnvironmentRoots(
        Scene scene,
        List<GameObject> protectedRoots)
    {
        List<GameObject> result = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null || protectedRoots.Contains(roots[i]))
                continue;

            result.Add(roots[i]);
        }

        return result;
    }

    private static bool IsProtectedRoot(GameObject root)
    {
        if (root == null)
            return false;

        for (int i = 0; i < ProtectedRootNames.Length; i++)
        {
            if (string.Equals(
                    root.name,
                    ProtectedRootNames[i],
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        for (int i = 0; i < ProtectedNameFragments.Length; i++)
        {
            if (root.name.IndexOf(
                    ProtectedNameFragments[i],
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return root.GetComponent<CarDriveScene>() != null ||
               root.GetComponent<CarDriveDestination>() != null ||
               root.GetComponent<CarDriveController>() != null ||
               root.GetComponent<CarCamera>() != null ||
               root.GetComponent<FirstPersonController>() != null ||
               root.GetComponent<StoryDirector>() != null ||
               root.GetComponent<DialogueSystem>() != null ||
               root.GetComponent<ObjectiveManager>() != null ||
               root.GetComponent<ShoppingCart>() != null ||
               root.GetComponent<QuestLogUI>() != null ||
               root.GetComponent<ToastUI>() != null ||
               root.GetComponent<PlayerInteractor>() != null ||
               root.GetComponent<CarInteraction>() != null;
    }

    private static void AppendNames(
        StringBuilder report,
        List<GameObject> objects,
        string indent)
    {
        if (objects.Count == 0)
        {
            report.AppendLine(indent + "(none)");
            return;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null)
                continue;

            report.Append(indent);
            report.Append(objects[i].name);

            if (PrefabUtility.IsPartOfPrefabInstance(objects[i]))
                report.Append("  [prefab instance]");

            report.AppendLine();
        }
    }

    private static void AppendEnvironmentDiff(
        StringBuilder report,
        List<GameObject> cityRoots,
        List<GameObject> drivingRoots)
    {
        Dictionary<string, GameObject> city = ToDictionary(cityRoots);
        Dictionary<string, GameObject> driving = ToDictionary(drivingRoots);
        List<string> onlyCity = new List<string>();
        List<string> onlyDriving = new List<string>();
        List<string> changed = new List<string>();

        foreach (KeyValuePair<string, GameObject> pair in city)
        {
            GameObject other;

            if (!driving.TryGetValue(pair.Key, out other))
            {
                onlyCity.Add(pair.Key);
                continue;
            }

            if (pair.Value.transform.position != other.transform.position ||
                pair.Value.transform.rotation != other.transform.rotation ||
                pair.Value.transform.localScale != other.transform.localScale ||
                pair.Value.transform.childCount != other.transform.childCount ||
                pair.Value.GetComponents<Component>().Length !=
                    other.GetComponents<Component>().Length)
            {
                changed.Add(pair.Key);
            }
        }

        foreach (KeyValuePair<string, GameObject> pair in driving)
        {
            if (!city.ContainsKey(pair.Key))
                onlyDriving.Add(pair.Key);
        }

        report.AppendLine("Environment differences:");
        AppendList(report, "  only in City: ", onlyCity);
        AppendList(report, "  only in CarDriving: ", onlyDriving);
        AppendList(report, "  transform/child/component differences: ", changed);

        if (onlyCity.Count == 0 &&
            onlyDriving.Count == 0 &&
            changed.Count == 0)
        {
            report.AppendLine("  none detected");
        }
    }

    private static void AppendList(
        StringBuilder report,
        string label,
        List<string> values)
    {
        if (values.Count == 0)
            return;

        report.Append(label);

        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0)
                report.Append(", ");

            report.Append(values[i]);
        }

        report.AppendLine();
    }

    private static Dictionary<string, GameObject> ToDictionary(
        List<GameObject> objects)
    {
        Dictionary<string, GameObject> result =
            new Dictionary<string, GameObject>(
                StringComparer.OrdinalIgnoreCase
            );

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null || result.ContainsKey(objects[i].name))
                continue;

            result.Add(objects[i].name, objects[i]);
        }

        return result;
    }

    private static void AppendScene(StringBuilder report, Scene scene)
    {
        report.AppendLine("=== " + scene.name + " (" + scene.path + ") ===");

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
            AppendTransform(report, roots[i].transform, 0);
    }

    private static void AppendTransform(
        StringBuilder report,
        Transform transform,
        int depth)
    {
        if (transform == null)
            return;

        report.Append(' ', depth * 2);
        report.Append("- ");
        report.Append(transform.name);
        report.Append("  [");

        Component[] components = transform.GetComponents<Component>();
        bool first = true;

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
                continue;

            if (!first)
                report.Append(", ");

            report.Append(component.GetType().Name);
            first = false;
        }

        report.AppendLine("]");

        for (int i = 0; i < transform.childCount; i++)
            AppendTransform(report, transform.GetChild(i), depth + 1);
    }

    private static void WriteReport(StringBuilder report, string fileName)
    {
        string path = LogPath(fileName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report.ToString());
            Debug.Log("DrivingSceneSyncTool: wrote " + path);
        }
        catch (Exception error)
        {
            Debug.LogError(
                "DrivingSceneSyncTool: could not write " + path + ": " + error
            );
        }
    }
}
#endif
