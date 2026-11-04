#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Collects every conversation from the story scenes and creates one
/// DialogueSequence asset per conversation under Assets/Dialogue. The scenes
/// are opened additively and are never saved, so Store.unity and City.unity
/// stay untouched.
///
/// Re-running the importer keeps existing text and voice clips. It only fills
/// empty fields and appends lines that were added to the scene later.
/// </summary>
public static class DialogueDatabaseImporter
{
    private const string DialogueFolder = "Assets/Dialogue";
    private const string ScenesFolder = "Assets/Scenes";

    private const string DatabasePath =
        "Assets/Resources/Paralysis/DialogueDatabase.asset";

    private const string DialogueConfigPath =
        "Assets/Resources/Paralysis/DialogueSystemConfig.asset";

    private const string DialoguePrefabPath =
        "Assets/Resources/Paralysis/DialogueCanvas.prefab";

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath) ?? ".";

    private sealed class Conversation
    {
        public string id;
        public DialogueLine[] lines;
        public bool lockMovement = true;
        public string firstSource;
        public int duplicateCount;
    }

    [MenuItem("Paralysis/Setup/Import Dialogue Database")]
    public static void ImportAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "DialogueDatabaseImporter: exit play mode before importing."
            );

            return;
        }

        StringBuilder report = new StringBuilder();

        try
        {
            EnsureFolder("Assets", "Dialogue");
            EnsureFolder("Assets/Resources", "Paralysis");

            Dictionary<string, Conversation> conversations =
                new Dictionary<string, Conversation>(
                    StringComparer.OrdinalIgnoreCase
                );

            ScanScenes(conversations, report);
            ScanSequenceAssets(conversations, report);

            List<DialogueSequence> sequences =
                CreateSequenceAssets(conversations, report);

            PopulateDatabase(sequences, report);
            ConfigureDialogueSystem();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DialogueDatabase.ClearCache();

            WriteVoiceReport(sequences, report);
            WriteReport(report.ToString());

            Debug.Log("DialogueDatabaseImporter: " + report);
        }
        catch (Exception error)
        {
            report.AppendLine("ERROR: " + error);
            Debug.LogError("DialogueDatabaseImporter: " + error);
            WriteReport(report.ToString());
        }
    }

    private static void ScanScenes(
        Dictionary<string, Conversation> conversations,
        StringBuilder report)
    {
        if (!Directory.Exists(ScenesFolder))
            return;

        string[] scenePaths = Directory.GetFiles(
            ScenesFolder,
            "*.unity",
            SearchOption.TopDirectoryOnly
        );

        Array.Sort(scenePaths, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string path = scenePaths[i].Replace('\\', '/');

            if (path.EndsWith("/MainMenu.unity", StringComparison.OrdinalIgnoreCase))
                continue;

            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedHere = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(
                    path,
                    OpenSceneMode.Additive
                );

                openedHere = true;
            }

            int before = conversations.Count;
            ScanScene(scene, conversations);
            report.AppendLine(
                "Scanned " + Path.GetFileName(path) + ": " +
                (conversations.Count - before) + " new conversation(s)."
            );

            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ScanScene(
        Scene scene,
        Dictionary<string, Conversation> conversations)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            MonoBehaviour[] behaviours =
                roots[i].GetComponentsInChildren<MonoBehaviour>(true);

            for (int j = 0; j < behaviours.Length; j++)
            {
                MonoBehaviour behaviour = behaviours[j];

                if (behaviour == null)
                    continue;

                Type type = behaviour.GetType();

                for (Type current = type;
                     current != null && current != typeof(MonoBehaviour);
                     current = current.BaseType)
                {
                    FieldInfo[] fields = current.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                    for (int k = 0; k < fields.Length; k++)
                    {
                        FieldInfo field = fields[k];

                        if (field.FieldType == typeof(DialogueSource))
                        {
                            DialogueSource source =
                                field.GetValue(behaviour) as DialogueSource;

                            Collect(
                                source,
                                behaviour.name + "." + field.Name,
                                conversations
                            );
                        }
                        else if (field.FieldType == typeof(DialogueSource[]))
                        {
                            DialogueSource[] array =
                                field.GetValue(behaviour) as DialogueSource[];

                            if (array == null)
                                continue;

                            for (int n = 0; n < array.Length; n++)
                            {
                                Collect(
                                    array[n],
                                    behaviour.name + "." + field.Name +
                                    "[" + n + "]",
                                    conversations
                                );
                            }
                        }
                        else if (field.FieldType ==
                                 typeof(List<DialogueSource>))
                        {
                            List<DialogueSource> list =
                                field.GetValue(behaviour) as
                                    List<DialogueSource>;

                            if (list == null)
                                continue;

                            for (int n = 0; n < list.Count; n++)
                            {
                                Collect(
                                    list[n],
                                    behaviour.name + "." + field.Name +
                                    "[" + n + "]",
                                    conversations
                                );
                            }
                        }
                    }
                }
            }
        }
    }

    private static void ScanSequenceAssets(
        Dictionary<string, Conversation> conversations,
        StringBuilder report)
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueSequence");
        int added = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            DialogueSequence sequence =
                AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);

            if (sequence == null || !sequence.HasLines)
                continue;

            int before = conversations.Count;

            CollectSequence(
                sequence,
                path,
                conversations
            );

            if (conversations.Count > before)
                added++;
        }

        report.AppendLine(
            "Scanned DialogueSequence assets: " + added +
            " new conversation(s)."
        );
    }

    private static void Collect(
        DialogueSource source,
        string sourceName,
        Dictionary<string, Conversation> conversations)
    {
        if (source == null)
            return;

        string id = source.ResolveId();

        if (string.IsNullOrEmpty(id))
            return;

        DialogueLine[] lines = source.sequence != null &&
                               source.sequence.HasLines
            ? source.sequence.lines
            : source.lines;

        if (lines == null || lines.Length == 0)
            return;

        bool lockMovement = source.sequence != null
            ? source.sequence.lockPlayerMovement
            : source.lockPlayerMovement;

        AddConversation(
            id,
            lines,
            lockMovement,
            sourceName,
            conversations
        );
    }

    private static void CollectSequence(
        DialogueSequence sequence,
        string sourceName,
        Dictionary<string, Conversation> conversations)
    {
        if (sequence == null || !sequence.HasLines)
            return;

        string id = sequence.conversationId;

        if (string.IsNullOrEmpty(id))
            id = sequence.name;

        AddConversation(
            id,
            sequence.lines,
            sequence.lockPlayerMovement,
            sourceName,
            conversations
        );
    }

    private static void AddConversation(
        string id,
        DialogueLine[] lines,
        bool lockMovement,
        string sourceName,
        Dictionary<string, Conversation> conversations)
    {
        Conversation existing;

        if (!conversations.TryGetValue(id, out existing))
        {
            conversations.Add(
                id,
                new Conversation
                {
                    id = id,
                    lines = CloneLines(lines),
                    lockMovement = lockMovement,
                    firstSource = sourceName
                }
            );

            return;
        }

        existing.duplicateCount++;

        // Different scene objects share an id (for example every grocery
        // item's "not needed" line). Keep the longest copy so no line is lost.
        if (lines != null && lines.Length > existing.lines.Length)
        {
            existing.lines = CloneLines(lines);
            existing.lockMovement = lockMovement;
            existing.firstSource = sourceName;
        }
    }

    private static List<DialogueSequence> CreateSequenceAssets(
        Dictionary<string, Conversation> conversations,
        StringBuilder report)
    {
        List<string> ids = new List<string>(conversations.Keys);
        ids.Sort(StringComparer.OrdinalIgnoreCase);

        List<DialogueSequence> sequences = new List<DialogueSequence>();

        for (int i = 0; i < ids.Count; i++)
        {
            Conversation conversation = conversations[ids[i]];
            string assetPath = DialogueFolder + "/" +
                               Sanitize(conversation.id) + ".asset";

            DialogueSequence sequence =
                AssetDatabase.LoadAssetAtPath<DialogueSequence>(assetPath);

            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<DialogueSequence>();
                sequence.name = Sanitize(conversation.id);
                sequence.conversationId = conversation.id;
                sequence.lockPlayerMovement = conversation.lockMovement;
                sequence.lines = CloneLines(conversation.lines);

                AssetDatabase.CreateAsset(sequence, assetPath);

                report.AppendLine(
                    "Created " + assetPath + " (" +
                    conversation.lines.Length + " line(s))."
                );
            }
            else
            {
                MergeLines(sequence, conversation.lines);
                EditorUtility.SetDirty(sequence);

                report.AppendLine(
                    "Kept " + assetPath + " (" +
                    (sequence.lines != null ? sequence.lines.Length : 0) +
                    " line(s))."
                );
            }

            if (conversation.duplicateCount > 0)
            {
                report.AppendLine(
                    "  Note: " + conversation.id + " was used by " +
                    (conversation.duplicateCount + 1) +
                    " scene sources; kept the longest copy."
                );
            }

            sequences.Add(sequence);
        }

        return sequences;
    }

    private static void MergeLines(
        DialogueSequence sequence,
        DialogueLine[] sceneLines)
    {
        if (sceneLines == null || sceneLines.Length == 0)
            return;

        if (sequence.lines == null || sequence.lines.Length == 0)
        {
            sequence.lines = CloneLines(sceneLines);
            return;
        }

        List<DialogueLine> merged =
            new List<DialogueLine>(sequence.lines);

        for (int i = 0; i < sceneLines.Length; i++)
        {
            DialogueLine sceneLine = sceneLines[i];

            if (i >= merged.Count)
            {
                merged.Add(
                    sceneLine != null
                        ? sceneLine.Clone()
                        : null
                );

                continue;
            }

            DialogueLine assetLine = merged[i];

            if (assetLine == null)
            {
                merged[i] = sceneLine != null
                    ? sceneLine.Clone()
                    : null;

                continue;
            }

            if (sceneLine == null)
                continue;

            // Fill only missing content so hand-edited text and assigned
            // voices are never overwritten by a re-import.
            if (string.IsNullOrEmpty(assetLine.speaker))
                assetLine.speaker = sceneLine.speaker;

            if (string.IsNullOrEmpty(assetLine.text))
                assetLine.text = sceneLine.text;

            if (assetLine.portrait == null)
                assetLine.portrait = sceneLine.portrait;

            if (assetLine.voiceClip == null)
                assetLine.voiceClip = sceneLine.voiceClip;

            if (assetLine.soundEffect == null)
                assetLine.soundEffect = sceneLine.soundEffect;
        }

        sequence.lines = merged.ToArray();
    }

    private static void PopulateDatabase(
        List<DialogueSequence> sequences,
        StringBuilder report)
    {
        DialogueDatabase database =
            AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<DialogueDatabase>();
            database.name = "DialogueDatabase";
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.sequences = new List<DialogueSequence>(sequences);
        database.RebuildLookup();
        EditorUtility.SetDirty(database);

        report.AppendLine(
            "DialogueDatabase: " + database.sequences.Count +
            " conversation(s)."
        );
    }

    private static void ConfigureDialogueSystem()
    {
        DialogueSystemConfig config =
            AssetDatabase.LoadAssetAtPath<DialogueSystemConfig>(
                DialogueConfigPath
            );

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<DialogueSystemConfig>();
            config.name = "DialogueSystemConfig";
            AssetDatabase.CreateAsset(config, DialogueConfigPath);
        }

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabPath);

        if (prefab != null)
            config.canvasPrefab = prefab;

        config.useAuthoredCanvas = prefab != null;
        EditorUtility.SetDirty(config);
    }

    private static void WriteVoiceReport(
        List<DialogueSequence> sequences,
        StringBuilder report)
    {
        List<DialogueSequence> ordered =
            new List<DialogueSequence>(sequences);

        ordered.Sort(
            (a, b) => string.Compare(
                a.name,
                b.name,
                StringComparison.OrdinalIgnoreCase
            )
        );

        StringBuilder voice = new StringBuilder();
        int totalLines = 0;
        int totalVoiced = 0;

        voice.AppendLine("PARALYSIS DIALOGUE VOICE COVERAGE");
        voice.AppendLine("Generated: " + DateTime.Now.ToString("u"));
        voice.AppendLine(
            "Voice clips are read from the DialogueSequence assets under " +
            "Assets/Dialogue. Re-run the importer after adding lines."
        );
        voice.AppendLine();

        for (int i = 0; i < ordered.Count; i++)
        {
            DialogueSequence sequence = ordered[i];
            DialogueLine[] lines = sequence.lines ?? new DialogueLine[0];
            int voiced = 0;
            int missing = 0;

            voice.AppendLine(
                sequence.name + "  (" + lines.Length + " line(s))"
            );

            for (int j = 0; j < lines.Length; j++)
            {
                DialogueLine line = lines[j];
                bool hasVoice = line != null && line.voiceClip != null;

                if (hasVoice)
                {
                    voiced++;
                }
                else
                {
                    missing++;
                }

                string text = line != null ? line.text : string.Empty;

                if (string.IsNullOrEmpty(text))
                    text = "(empty)";

                if (text.Length > 70)
                    text = text.Substring(0, 67) + "...";

                voice.AppendLine(
                    "  [" + (j + 1).ToString("00") + "] " +
                    (hasVoice ? "VOICE  " : "MISSING") +
                    "  " + text
                );
            }

            voice.AppendLine(
                "  -> " + voiced + " voiced, " + missing + " missing"
            );
            voice.AppendLine();

            totalLines += lines.Length;
            totalVoiced += voiced;
        }

        voice.AppendLine(
            "TOTAL: " + totalVoiced + " / " + totalLines +
            " line(s) have a voice clip."
        );

        try
        {
            string logs = Path.Combine(ProjectRoot, "Logs");

            if (!Directory.Exists(logs))
                Directory.CreateDirectory(logs);

            File.WriteAllText(
                Path.Combine(logs, "dialogue_voice_report.txt"),
                voice.ToString()
            );
        }
        catch (Exception error)
        {
            report.AppendLine(
                "Could not write voice report: " + error.Message
            );
        }

        report.AppendLine(
            "Voice coverage: " + totalVoiced + " / " + totalLines +
            " line(s)."
        );
    }

    private static DialogueLine[] CloneLines(DialogueLine[] source)
    {
        if (source == null)
            return null;

        DialogueLine[] copy = new DialogueLine[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            DialogueLine line = source[i];

            copy[i] = line != null
                ? line.Clone()
                : null;
        }

        return copy;
    }

    private static string Sanitize(string id)
    {
        string name = string.IsNullOrEmpty(id) ? "Dialogue" : id;

        char[] invalid = Path.GetInvalidFileNameChars();

        for (int i = 0; i < invalid.Length; i++)
            name = name.Replace(invalid[i], '_');

        return name;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (AssetDatabase.IsValidFolder(parent + "/" + child))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
        {
            int slash = parent.LastIndexOf('/');

            if (slash > 0)
                EnsureFolder(parent.Substring(0, slash), parent.Substring(slash + 1));
        }

        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
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
                Path.Combine(logs, "dialogue_import.txt"),
                report
            );
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "DialogueDatabaseImporter: could not write report: " +
                error.Message
            );
        }
    }
}
#endif
