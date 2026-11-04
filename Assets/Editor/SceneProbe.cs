#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Temporary/editor-only diagnostics for the story scenes. Run from the menu
/// or through Temp/probe_scenes.flag after a script reload.
/// </summary>
[NoAutoStaticsCleanup]
public static class SceneProbe
{
    private const string FlagPath = "Temp/probe_scenes.flag";
    private const string LogPath = "Temp/scene_probe.txt";

    [MenuItem("Paralysis/Testing/Probe Story Scenes")]
    public static void ProbeFromMenu()
    {
        Run();
    }

    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string exitFlag = Path.Combine(root, "Temp/exit_play.flag");

        if (File.Exists(exitFlag))
        {
            try
            {
                File.Delete(exitFlag);
            }
            catch (IOException)
            {
            }

            File.WriteAllText(
                Path.Combine(root, "Temp/exit_play_status.txt"),
                "isPlaying=" + EditorApplication.isPlaying +
                " isPlayingOrWillChangePlaymode=" +
                EditorApplication.isPlayingOrWillChangePlaymode +
                " isCompiling=" + EditorApplication.isCompiling
            );

            Debug.Log(
                "SceneProbe: exit requested. isPlaying=" +
                EditorApplication.isPlaying +
                " isPlayingOrWillChangePlaymode=" +
                EditorApplication.isPlayingOrWillChangePlaymode
            );

            if (EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
            }

            return;
        }

        string flag = Path.Combine(root, FlagPath);

        if (!File.Exists(flag))
            return;

        try
        {
            File.Delete(flag);
        }
        catch (IOException)
        {
        }

        Run();
    }

    private static void Run()
    {
        StringBuilder log = new StringBuilder();

        log.AppendLine("=== OPEN SCENES ===");

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            log.AppendLine(
                open.name + " path=" + open.path +
                " loaded=" + open.isLoaded +
                " dirty=" + open.isDirty
            );
        }

        log.AppendLine();
        log.AppendLine("=== ACTIVE SCENE LISTENERS ===");

        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == null)
                continue;

            log.AppendLine(
                listeners[i].gameObject.scene.name + " / " +
                GetPath(listeners[i].transform) +
                " enabled=" + listeners[i].enabled +
                " activeInHierarchy=" + listeners[i].gameObject.activeInHierarchy
            );
        }

        ProbeScene("Assets/Scenes/Store.unity", log, false);
        ProbeDrivingScene(log);

        string rootPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..")
        );
        string output = Path.Combine(rootPath, LogPath);

        File.WriteAllText(output, log.ToString());
        Debug.Log("SceneProbe: wrote " + output + "\n" + log);
    }

    private static void ProbeScene(string path, StringBuilder log, bool dumpRoads)
    {
        log.AppendLine();
        log.AppendLine("=== PROBE " + path + " ===");

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        log.AppendLine("loaded=" + scene.isLoaded + " dirty=" + scene.isDirty);

        MissingScriptReport(scene, log);

        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include
        );

        int sceneListeners = 0;

        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == null ||
                listeners[i].gameObject.scene != scene)
            {
                continue;
            }

            sceneListeners++;
            log.AppendLine(
                "listener: " + GetPath(listeners[i].transform) +
                " enabled=" + listeners[i].enabled +
                " active=" + listeners[i].gameObject.activeInHierarchy
            );
        }

        log.AppendLine("listener count=" + sceneListeners);

        FirstPersonController player = FindInScene<FirstPersonController>(scene);

        if (player != null)
        {
            log.AppendLine(
                "player: " + GetPath(player.transform) +
                " pos=" + player.transform.position.ToString("F3") +
                " rot=" + player.transform.eulerAngles.ToString("F1") +
                " scale=" + player.transform.lossyScale.ToString("F3")
            );

            Camera cam = player.GetComponentInChildren<Camera>(true);

            if (cam != null)
            {
                log.AppendLine(
                    "player camera: " + GetPath(cam.transform) +
                    " localPos=" + cam.transform.localPosition.ToString("F3") +
                    " fov=" + cam.fieldOfView
                );
            }
        }
        else
        {
            log.AppendLine("player: NONE");
        }

        GroceryItem[] groceries = Object.FindObjectsByType<GroceryItem>(
            FindObjectsInactive.Include
        );

        int groceryCount = 0;

        for (int i = 0; i < groceries.Length; i++)
        {
            if (groceries[i] == null || groceries[i].gameObject.scene != scene)
                continue;

            groceryCount++;
            log.AppendLine(
                "grocery: " + GetPath(groceries[i].transform) +
                " id=" + GetSerializedString(groceries[i], "itemId") +
                " pos=" + groceries[i].transform.position.ToString("F3")
            );
        }

        log.AppendLine("grocery count=" + groceryCount);

        CashierInteraction cashier = FindInScene<CashierInteraction>(scene);

        if (cashier != null)
        {
            log.AppendLine(
                "cashier: " + GetPath(cashier.transform) +
                " pos=" + cashier.transform.position.ToString("F3")
            );

            SerializedObject so = new SerializedObject(cashier);
            LogReference(so, "normalModel", log);
            LogReference(so, "transformedModel", log);
            LogReference(so, "interactionPoint", log);
            LogReference(so, "audioSource", log);
            LogReference(so, "scannerSource", log);
            LogReference(so, "ambienceSource", log);
        }

        StoryDirector director = FindInScene<StoryDirector>(scene);

        if (director != null)
        {
            SerializedObject so = new SerializedObject(director);
            LogString(so, "apartmentScene", log);
            LogString(so, "cityScene", log);
            LogString(so, "storeScene", log);
            LogReference(so, "cart", log);
            LogReference(so, "lightsObjective", log);
        }

        if (dumpRoads)
        {
            DumpDrivingGeometry(scene, log);
        }

        if (previous.IsValid() && previous.isLoaded)
            SceneManager.SetActiveScene(previous);

        EditorSceneManager.CloseScene(scene, true);
    }

    private static void ProbeDrivingScene(StringBuilder log)
    {
        log.AppendLine();
        log.AppendLine("=== PROBE Assets/Scenes/CarDriving.unity ===");

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/CarDriving.unity",
            OpenSceneMode.Additive
        );

        SceneManager.SetActiveScene(scene);

        FirstPersonController player = FindInScene<FirstPersonController>(scene);

        if (player != null)
        {
            log.AppendLine(
                "walking player: " + GetPath(player.transform) +
                " pos=" + player.transform.position.ToString("F3")
            );
        }

        Transform car = null;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length && car == null; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);

            for (int j = 0; j < all.Length; j++)
            {
                if (all[j].name == "Free Racing Car")
                {
                    car = all[j].root;
                    break;
                }
            }
        }

        if (car == null)
        {
            log.AppendLine("car: NONE");
        }
        else
        {
            log.AppendLine(
                "car: " + GetPath(car) +
                " pos=" + car.position.ToString("F3") +
                " rot=" + car.eulerAngles.ToString("F1") +
                " forward=" + car.forward.ToString("F3") +
                " scale=" + car.lossyScale.ToString("F3")
            );
        }

        DumpDrivingGeometry(scene, log);

        if (previous.IsValid() && previous.isLoaded)
            SceneManager.SetActiveScene(previous);

        EditorSceneManager.CloseScene(scene, true);
    }

    private static void DumpDrivingGeometry(Scene scene, StringBuilder log)
    {
        log.AppendLine("--- road/renderers near car ---");

        Vector3 carPos = new Vector3(206.67361f, 65.36159f, -252.58868f);

        List<(string name, Vector3 pos, Bounds bounds)> items =
            new List<(string, Vector3, Bounds)>();

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null ||
                renderers[i].gameObject.scene != scene)
            {
                continue;
            }

            string name = renderers[i].gameObject.name;

            if (name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Plane", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Sidewalk", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Cross", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            items.Add((
                name,
                renderers[i].transform.position,
                renderers[i].bounds
            ));
        }

        items.Sort((a, b) =>
            Vector3.Distance(a.pos, carPos).CompareTo(
                Vector3.Distance(b.pos, carPos)
            )
        );

        for (int i = 0; i < items.Count && i < 60; i++)
        {
            log.AppendLine(
                items[i].name +
                " pos=" + items[i].pos.ToString("F2") +
                " dist=" + Vector3.Distance(items[i].pos, carPos).ToString("F1") +
                " bounds=" + items[i].bounds.center.ToString("F1") +
                "/" + items[i].bounds.size.ToString("F1")
            );
        }

        log.AppendLine("--- forward samples ---");

        for (int i = 1; i <= 12; i++)
        {
            Vector3 point = carPos + Vector3.right * (i * 15f);
            bool hit = Physics.Raycast(
                point + Vector3.up * 20f,
                Vector3.down,
                out RaycastHit info,
                80f
            );

            log.AppendLine(
                "x+" + (i * 15) + " point=" + point.ToString("F1") +
                " hit=" + (hit ? info.collider.name : "none") +
                " y=" + (hit ? info.point.y.ToString("F1") : "-")
            );
        }

        log.AppendLine("--- objects with store/shop/market ---");

        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == null ||
                transforms[i].gameObject.scene != scene)
            {
                continue;
            }

            string name = transforms[i].name;

            if (name.IndexOf("Store", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Shop", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Market", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                log.AppendLine(
                    name + " pos=" + transforms[i].position.ToString("F1") +
                    " path=" + GetPath(transforms[i])
                );
            }
        }
    }

    private static void MissingScriptReport(Scene scene, StringBuilder log)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        int missing = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);

            for (int j = 0; j < all.Length; j++)
            {
                Component[] components = all[j].GetComponents<Component>();

                for (int k = 0; k < components.Length; k++)
                {
                    if (components[k] == null)
                    {
                        missing++;
                        log.AppendLine("missing script on " + GetPath(all[j]));
                    }
                }
            }
        }

        log.AppendLine("missing scripts=" + missing);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include);

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene == scene)
                return all[i];
        }

        return null;
    }

    private static string GetPath(Transform t)
    {
        if (t == null)
            return "<null>";

        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    private static string GetSerializedString(Object target, string name)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        return p != null ? p.stringValue : "<missing>";
    }

    private static void LogString(SerializedObject so, string name, StringBuilder log)
    {
        SerializedProperty p = so.FindProperty(name);

        if (p != null)
            log.AppendLine(name + "=" + p.stringValue);
    }

    private static void LogReference(SerializedObject so, string name, StringBuilder log)
    {
        SerializedProperty p = so.FindProperty(name);

        if (p == null)
        {
            log.AppendLine(name + "=<missing property>");
            return;
        }

        Object value = p.objectReferenceValue;

        log.AppendLine(
            name + "=" + (value != null
                ? value.name + " (" + value.GetType().Name + ")"
                : "<null>")
        );
    }
}
#endif
