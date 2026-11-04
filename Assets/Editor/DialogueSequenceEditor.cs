#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for the editable conversation assets. It keeps the normal line
/// fields, but groups the voice track together with the exact file name the
/// automatic assigner looks for, so per-line recordings are easy to add.
/// </summary>
[CustomEditor(typeof(DialogueSequence))]
public class DialogueSequenceEditor : Editor
{
    private const string VoiceFolder = "Assets/Sounds/Speeches";

    private static readonly string[] Extensions =
    {
        ".wav",
        ".mp3",
        ".ogg"
    };

    private SerializedProperty conversationId;
    private SerializedProperty lockPlayerMovement;
    private SerializedProperty lines;
    private bool[] expanded = new bool[0];

    private void OnEnable()
    {
        conversationId = serializedObject.FindProperty("conversationId");
        lockPlayerMovement = serializedObject.FindProperty("lockPlayerMovement");
        lines = serializedObject.FindProperty("lines");
        EnsureExpanded();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EnsureExpanded();

        EditorGUILayout.PropertyField(conversationId);
        EditorGUILayout.PropertyField(lockPlayerMovement);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lines", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Every line can have its own voice track. Drag an AudioClip into " +
            "Voice Clip, or put <SequenceName>_01.wav, _02.wav ... in " +
            VoiceFolder + " and run Assign Missing Voice Clips.",
            MessageType.Info
        );

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Assign Missing Voice Clips"))
            {
                // Refresh/reimport can reload this asset, so wait until the
                // current GUI pass has finished.
                EditorApplication.delayCall +=
                    DialogueVoiceAssigner.AssignMissing;
            }

            if (GUILayout.Button("Open Speeches Folder"))
                EditorUtility.RevealInFinder(VoiceFolder);
        }

        EditorGUILayout.Space();

        for (int i = 0; i < lines.arraySize; i++)
        {
            DrawLine(i);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Line"))
        {
            lines.arraySize++;
            EnsureExpanded();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawLine(int index)
    {
        SerializedProperty line = lines.GetArrayElementAtIndex(index);
        SerializedProperty speaker = line.FindPropertyRelative("speaker");
        SerializedProperty text = line.FindPropertyRelative("text");

        string speakerLabel = string.IsNullOrEmpty(speaker.stringValue)
            ? "(no speaker)"
            : speaker.stringValue;

        string preview = text.stringValue ?? string.Empty;

        if (preview.Length > 56)
            preview = preview.Substring(0, 56) + "...";

        string title =
            (index + 1).ToString("00") + "  " + speakerLabel +
            (preview.Length > 0 ? "  -  " + preview : string.Empty);

        expanded[index] = EditorGUILayout.Foldout(
            expanded[index],
            title,
            true
        );

        if (!expanded[index])
            return;

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(speaker);
        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("speakerColor")
        );
        EditorGUILayout.PropertyField(text);
        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("portrait")
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Voice Track", EditorStyles.boldLabel);

        SerializedProperty voiceClip =
            line.FindPropertyRelative("voiceClip");

        EditorGUILayout.PropertyField(voiceClip);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(
                ExpectedName(index + 1) + ".wav / .mp3 / .ogg",
                EditorStyles.miniLabel
            );

            if (GUILayout.Button("Find In Speeches", GUILayout.Width(130f)))
            {
                string path = FindClipPath(index + 1);

                if (path == null)
                {
                    EditorUtility.DisplayDialog(
                        "No voice file",
                        "Expected " + ExpectedName(index + 1) +
                        ".wav (or .mp3/.ogg) in " + VoiceFolder + ".",
                        "OK"
                    );
                }
                else
                {
                    voiceClip.objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60f)))
                voiceClip.objectReferenceValue = null;
        }

        if (voiceClip.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "No voice track assigned. This line is text-only.",
                MessageType.None
            );
        }

        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("voiceVolume")
        );
        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("soundEffect")
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("delayBefore")
        );
        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("overrideTypewriter")
        );

        if (line.FindPropertyRelative("overrideTypewriter").boolValue)
        {
            EditorGUILayout.PropertyField(
                line.FindPropertyRelative("useTypewriter")
            );
        }

        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("pauseAfter")
        );
        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("movementLock")
        );
        EditorGUILayout.PropertyField(
            line.FindPropertyRelative("onLineFinished")
        );

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    private void EnsureExpanded()
    {
        if (expanded.Length == lines.arraySize)
            return;

        bool[] previous = expanded;
        expanded = new bool[lines.arraySize];

        for (int i = 0; i < expanded.Length && i < previous.Length; i++)
            expanded[i] = previous[i];
    }

    private string ExpectedName(int lineNumber)
    {
        return target.name + "_" + lineNumber.ToString("00");
    }

    private string FindClipPath(int lineNumber)
    {
        string baseName = ExpectedName(lineNumber);
        string projectRoot =
            Path.GetDirectoryName(Application.dataPath) ?? ".";

        for (int i = 0; i < Extensions.Length; i++)
        {
            string path = VoiceFolder + "/" + baseName + Extensions[i];

            if (File.Exists(Path.Combine(projectRoot, path)))
                return path;
        }

        return null;
    }
}
#endif
