#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Friendly Inspector for the music library. It shows each cue as a foldout,
/// lets you preview a track in Play Mode, and can restore the built-in story
/// cues and scene bindings if they are removed by accident.
/// </summary>
[CustomEditor(typeof(MusicDatabase))]
public class MusicDatabaseEditor : Editor
{
    private const string AssetPath =
        "Assets/Resources/Paralysis/MusicDatabase.asset";

    private SerializedProperty cues;
    private SerializedProperty scenes;

    private readonly List<bool> cueExpanded = new List<bool>();
    private readonly List<bool> sceneExpanded = new List<bool>();

    /// <summary>
    /// Restores the shipped one-shot mode for the timed songs. The grocery
    /// theme, the car arrival sting and the ending theme must not loop for the
    /// rest of their scene.
    /// </summary>
    [MenuItem("Paralysis/Music/Apply Timed Cue Defaults")]
    public static void ApplyTimedCueDefaults()
    {
        MusicDatabase database =
            AssetDatabase.LoadAssetAtPath<MusicDatabase>(AssetPath);

        if (database == null)
        {
            Debug.LogWarning(
                "MusicDatabaseEditor: could not find " + AssetPath
            );

            return;
        }

        bool changed = false;

        changed |= SetLoop(database, MusicCueIds.Store, false);
        changed |= SetLoop(database, MusicCueIds.CarArrival, false);
        changed |= SetLoop(database, MusicCueIds.End, false);

        if (!changed)
        {
            Debug.Log(
                "MusicDatabaseEditor: timed cues are already one-shot."
            );

            return;
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        MusicDatabase.ClearCache();

        Debug.Log("MusicDatabaseEditor: timed cues set to one-shot.");
    }

    private static bool SetLoop(
        MusicDatabase database,
        string cueId,
        bool loop)
    {
        MusicCue cue = database.FindCue(cueId);

        if (cue == null || cue.loop == loop)
            return false;

        cue.loop = loop;
        return true;
    }

    private void OnEnable()
    {
        cues = serializedObject.FindProperty("cues");
        scenes = serializedObject.FindProperty("scenes");
        EnsureExpanded();
    }

    public override void OnInspectorGUI()
    {
        MusicDatabase database = (MusicDatabase)target;

        serializedObject.Update();
        EnsureExpanded();

        EditorGUILayout.HelpBox(
            "Assign one AudioClip per cue. Scene bindings choose what plays " +
            "when a scene loads. Story beats (lights off, checkout, scary " +
            "transformation) request their cue ids automatically. A cue with " +
            "no clip keeps the current music playing.",
            MessageType.Info
        );

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "cues",
            "scenes"
        );

        EditorGUILayout.Space();
        DrawCues(database);

        EditorGUILayout.Space();
        DrawScenes(database);

        EditorGUILayout.Space();
        bool changed = DrawActions(database);

        if (serializedObject.ApplyModifiedProperties() || changed)
            MusicDatabase.ClearCache();

        if (database != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                database.AssignedClipCount() + " of " +
                (database.cues != null ? database.cues.Count : 0) +
                " cues have a clip assigned. See Docs/Music.md for the full " +
                "workflow.",
                MessageType.None
            );
        }
    }

    private void DrawCues(MusicDatabase database)
    {
        EditorGUILayout.LabelField("Cues", EditorStyles.boldLabel);

        for (int i = 0; i < cues.arraySize; i++)
        {
            if (DrawCue(i, database))
                break;
        }

        if (GUILayout.Button("Add Cue"))
        {
            AddCue(
                "new_cue",
                "New Cue",
                "",
                0.65f,
                true,
                1.5f,
                1f
            );

            EnsureExpanded();
        }
    }

    /// <summary>Returns true when the cue was removed.</summary>
    private bool DrawCue(int index, MusicDatabase database)
    {
        SerializedProperty cue = cues.GetArrayElementAtIndex(index);
        SerializedProperty id = cue.FindPropertyRelative("id");
        SerializedProperty displayName =
            cue.FindPropertyRelative("displayName");
        SerializedProperty loop = cue.FindPropertyRelative("loop");

        string title = (index + 1).ToString("00") + "  " +
            (string.IsNullOrEmpty(id.stringValue)
                ? "(no id)"
                : id.stringValue);

        if (!string.IsNullOrEmpty(displayName.stringValue))
            title += "  -  " + displayName.stringValue;

        title += loop.boolValue ? "   [loop]" : "   [one-shot]";

        cueExpanded[index] = EditorGUILayout.Foldout(
            cueExpanded[index],
            title,
            true
        );

        if (!cueExpanded[index])
            return false;

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(id);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("description"));
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("clip"));
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("volume"));
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("loop"));
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("fadeIn"));
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("fadeOut"));
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("pitch"));

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Optional Stinger", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(cue.FindPropertyRelative("stinger"));
        EditorGUILayout.PropertyField(
            cue.FindPropertyRelative("stingerVolume")
        );

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(
                       !Application.isPlaying ||
                       string.IsNullOrEmpty(id.stringValue)))
            {
                if (GUILayout.Button("Preview"))
                    MusicManager.PlayCue(id.stringValue);
            }

            if (GUILayout.Button("Remove"))
            {
                cues.DeleteArrayElementAtIndex(index);
                EnsureExpanded();
                EditorGUI.indentLevel--;
                return true;
            }
        }

        EditorGUI.indentLevel--;
        return false;
    }

    private void DrawScenes(MusicDatabase database)
    {
        EditorGUILayout.LabelField("Scene Bindings", EditorStyles.boldLabel);

        for (int i = 0; i < scenes.arraySize; i++)
        {
            if (DrawSceneBinding(i, database))
                break;
        }

        if (GUILayout.Button("Add Scene Binding"))
        {
            AddSceneBinding("", "", false);
            EnsureExpanded();
        }
    }

    /// <summary>Returns true when the binding was removed.</summary>
    private bool DrawSceneBinding(int index, MusicDatabase database)
    {
        SerializedProperty binding = scenes.GetArrayElementAtIndex(index);
        SerializedProperty sceneName =
            binding.FindPropertyRelative("sceneName");
        SerializedProperty cueId = binding.FindPropertyRelative("cueId");

        string title = (index + 1).ToString("00") + "  " +
            (string.IsNullOrEmpty(sceneName.stringValue)
                ? "(no scene)"
                : sceneName.stringValue) +
            "  ->  " +
            (string.IsNullOrEmpty(cueId.stringValue)
                ? "(no cue)"
                : cueId.stringValue);

        sceneExpanded[index] = EditorGUILayout.Foldout(
            sceneExpanded[index],
            title,
            true
        );

        if (!sceneExpanded[index])
            return false;

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(sceneName);
        DrawCuePopup(cueId, database);
        EditorGUILayout.PropertyField(
            binding.FindPropertyRelative("restartWhenLoaded")
        );

        if (GUILayout.Button("Remove Binding"))
        {
            scenes.DeleteArrayElementAtIndex(index);
            EnsureExpanded();
            EditorGUI.indentLevel--;
            return true;
        }

        EditorGUI.indentLevel--;
        return false;
    }

    private static void DrawCuePopup(
        SerializedProperty cueId,
        MusicDatabase database)
    {
        List<string> ids = new List<string>();

        if (database != null && database.cues != null)
        {
            for (int i = 0; i < database.cues.Count; i++)
            {
                MusicCue cue = database.cues[i];

                if (cue != null && !string.IsNullOrEmpty(cue.id))
                    ids.Add(cue.id);
            }
        }

        string[] options = new string[ids.Count + 1];
        options[0] = "(none)";

        int current = 0;

        for (int i = 0; i < ids.Count; i++)
        {
            options[i + 1] = ids[i];

            if (string.Equals(
                    ids[i],
                    cueId.stringValue,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                current = i + 1;
            }
        }

        int selected = EditorGUILayout.Popup(
            new GUIContent("Cue Id"),
            current,
            options
        );

        if (selected != current)
        {
            cueId.stringValue = selected == 0
                ? ""
                : ids[selected - 1];
        }
    }

    private bool DrawActions(MusicDatabase database)
    {
        bool changed = false;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Missing Story Cues"))
            {
                AddMissingStoryCues();
                changed = true;
            }

            if (GUILayout.Button("Add Missing Scene Bindings"))
            {
                AddMissingSceneBindings();
                changed = true;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Validate"))
                ValidateDatabase(database);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Stop Music"))
                    MusicManager.StopMusic(0.5f);
            }
        }

        return changed;
    }

    private void AddMissingStoryCues()
    {
        MusicDatabase database = (MusicDatabase)target;

        for (int i = 0; i < MusicCueIds.All.Length; i++)
        {
            string id = MusicCueIds.All[i];

            if (database.FindCue(id) != null)
                continue;

            AddCueForId(id);
        }

        EnsureExpanded();
    }

    private void AddCueForId(string id)
    {
        string displayName = id;
        string description = "";
        float volume = 0.65f;
        bool loop = true;
        float fadeIn = 1.5f;
        float fadeOut = 1f;

        switch (id)
        {
            case MusicCueIds.Menu:
                displayName = "Main Menu";
                description = "Theme for the main menu and chapter selection.";
                volume = 0.6f;
                break;

            case MusicCueIds.Start:
                displayName = "Act 1 - Apartment Start";
                description = "Opening theme when the apartment scene loads.";
                volume = 0.55f;
                fadeIn = 2f;
                break;

            case MusicCueIds.HomeLightsOff:
                displayName = "Act 1 - After The Lights";
                description = "Optional cue after the last light is switched off.";
                volume = 0.6f;
                break;

            case MusicCueIds.City:
                displayName = "Act 2 - City";
                description = "Theme when the city street scene loads.";
                volume = 0.55f;
                break;

            case MusicCueIds.CarArc:
                displayName = "Act 3 - Car Arc";
                description = "Driving theme for the CarDriving scene.";
                volume = 0.6f;
                break;

            case MusicCueIds.CarArrival:
                displayName = "Act 3 - Car Arrival";
                description = "Optional sting when the car reaches the store.";
                volume = 0.7f;
                loop = false;
                fadeIn = 0.25f;
                fadeOut = 0.5f;
                break;

            case MusicCueIds.Store:
                displayName = "Act 4 - Store";
                description =
                    "Timed grocery theme for the store scene. Plays once.";
                volume = 0.55f;
                loop = false;
                break;

            case MusicCueIds.StoreReadyToPay:
                displayName = "Act 4 - Ready To Pay";
                description = "Optional cue after every grocery is collected.";
                volume = 0.6f;
                break;

            case MusicCueIds.StoreCheckout:
                displayName = "Act 4 - Checkout";
                description = "Optional tense cue when checkout starts.";
                volume = 0.65f;
                fadeIn = 1f;
                fadeOut = 1f;
                break;

            case MusicCueIds.ScaryCheckout:
                displayName = "Act 4 - Scary Checkout";
                description = "Horror cue when the cashier transforms.";
                volume = 0.7f;
                fadeIn = 0.4f;
                fadeOut = 0.8f;
                break;

            case MusicCueIds.End:
                displayName = "Ending - Demo End";
                description =
                    "Optional theme for the end-of-demo screen. Plays once.";
                volume = 0.6f;
                loop = false;
                fadeIn = 2f;
                fadeOut = 1.5f;
                break;
        }

        AddCue(
            id,
            displayName,
            description,
            volume,
            loop,
            fadeIn,
            fadeOut
        );
    }

    private void AddCue(
        string id,
        string displayName,
        string description,
        float volume,
        bool loop,
        float fadeIn,
        float fadeOut)
    {
        int index = cues.arraySize;
        cues.arraySize = index + 1;

        SerializedProperty cue = cues.GetArrayElementAtIndex(index);
        cue.FindPropertyRelative("id").stringValue = id;
        cue.FindPropertyRelative("displayName").stringValue = displayName;
        cue.FindPropertyRelative("description").stringValue = description;
        cue.FindPropertyRelative("clip").objectReferenceValue = null;
        cue.FindPropertyRelative("volume").floatValue = volume;
        cue.FindPropertyRelative("loop").boolValue = loop;
        cue.FindPropertyRelative("fadeIn").floatValue = fadeIn;
        cue.FindPropertyRelative("fadeOut").floatValue = fadeOut;
        cue.FindPropertyRelative("pitch").floatValue = 1f;
        cue.FindPropertyRelative("stinger").objectReferenceValue = null;
        cue.FindPropertyRelative("stingerVolume").floatValue = 0.8f;
    }

    private void AddMissingSceneBindings()
    {
        string[] sceneNames =
        {
            "MainMenu",
            "Home",
            "City",
            "CarDriving",
            "Store",
            "End"
        };

        string[] cueIds =
        {
            MusicCueIds.Menu,
            MusicCueIds.Start,
            MusicCueIds.City,
            MusicCueIds.CarArc,
            MusicCueIds.Store,
            MusicCueIds.End
        };

        MusicDatabase database = (MusicDatabase)target;

        for (int i = 0; i < sceneNames.Length; i++)
        {
            if (database.FindScene(sceneNames[i]) != null)
                continue;

            AddSceneBinding(sceneNames[i], cueIds[i], false);
        }

        EnsureExpanded();
    }

    private void AddSceneBinding(
        string sceneName,
        string cueId,
        bool restartWhenLoaded)
    {
        int index = scenes.arraySize;
        scenes.arraySize = index + 1;

        SerializedProperty binding = scenes.GetArrayElementAtIndex(index);
        binding.FindPropertyRelative("sceneName").stringValue = sceneName;
        binding.FindPropertyRelative("cueId").stringValue = cueId;
        binding.FindPropertyRelative("restartWhenLoaded").boolValue =
            restartWhenLoaded;
    }

    private void EnsureExpanded()
    {
        while (cueExpanded.Count < cues.arraySize)
            cueExpanded.Add(false);

        if (cueExpanded.Count > cues.arraySize)
        {
            cueExpanded.RemoveRange(
                cues.arraySize,
                cueExpanded.Count - cues.arraySize
            );
        }

        while (sceneExpanded.Count < scenes.arraySize)
            sceneExpanded.Add(false);

        if (sceneExpanded.Count > scenes.arraySize)
        {
            sceneExpanded.RemoveRange(
                scenes.arraySize,
                sceneExpanded.Count - scenes.arraySize
            );
        }
    }

    private static void ValidateDatabase(MusicDatabase database)
    {
        if (database == null)
            return;

        List<string> issues = database.Validate();

        if (issues.Count == 0)
        {
            Debug.Log("MusicDatabase: no issues found.", database);
            return;
        }

        for (int i = 0; i < issues.Count; i++)
            Debug.LogWarning("MusicDatabase: " + issues[i], database);
    }

    [MenuItem("Paralysis/Music/Select Music Database")]
    private static void SelectDatabase()
    {
        MusicDatabase database =
            AssetDatabase.LoadAssetAtPath<MusicDatabase>(AssetPath);

        if (database == null)
        {
            Debug.LogWarning(
                "MusicDatabaseEditor: no database found at " + AssetPath
            );

            return;
        }

        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }

    [MenuItem("Paralysis/Music/Validate Music Database")]
    private static void ValidateDatabase()
    {
        MusicDatabase database =
            AssetDatabase.LoadAssetAtPath<MusicDatabase>(AssetPath);

        if (database == null)
        {
            Debug.LogWarning(
                "MusicDatabaseEditor: no database found at " + AssetPath
            );

            return;
        }

        ValidateDatabase(database);
    }
}
#endif
