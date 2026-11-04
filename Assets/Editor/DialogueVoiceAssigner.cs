#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assigns voice clips to the editable DialogueSequence assets by file name.
/// A line at index 01 of APT_Opening_01 looks for
/// Assets/Sounds/Speeches/APT_Opening_01_01.wav (mp3 and ogg also work).
///
/// Replacing a placeholder WAV with a real recording at the same path needs no
/// re-import: Unity updates the AudioClip in place. The missing-only pass is
/// safe to run again after adding new lines.
/// </summary>
public static class DialogueVoiceAssigner
{
    private const string DialogueFolder = "Assets/Dialogue";
    private const string VoiceFolder = "Assets/Sounds/Speeches";

    private static readonly string[] Extensions =
    {
        ".wav",
        ".mp3",
        ".ogg"
    };

    private static string ProjectRoot =>
        Path.GetDirectoryName(Application.dataPath) ?? ".";

    [MenuItem("Paralysis/Setup/Assign Dialogue Voice Clips")]
    public static void AssignMissing()
    {
        Assign(false);
    }

    [MenuItem("Paralysis/Setup/Assign Dialogue Voice Clips (Overwrite)")]
    public static void AssignAll()
    {
        Assign(true);
    }

    private static void Assign(bool overwrite)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "DialogueVoiceAssigner: exit play mode before assigning."
            );

            return;
        }

        StringBuilder report = new StringBuilder();
        int assigned = 0;
        int kept = 0;
        int missing = 0;

        report.AppendLine("PARALYSIS DIALOGUE VOICE ASSIGNMENT");
        report.AppendLine("Generated: " + DateTime.UtcNow.ToString("u"));
        report.AppendLine(
            "Mode: " + (overwrite ? "overwrite" : "missing only")
        );
        report.AppendLine("Voice folder: " + VoiceFolder);
        report.AppendLine();

        try
        {
            // Pick up WAVs copied into the project since the last refresh.
            AssetDatabase.Refresh();

            string[] guids = AssetDatabase.FindAssets(
                "t:DialogueSequence",
                new[] { DialogueFolder }
            );

            List<DialogueSequence> sequences =
                new List<DialogueSequence>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DialogueSequence sequence =
                    AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);

                if (sequence != null)
                    sequences.Add(sequence);
            }

            sequences.Sort(
                (a, b) => string.Compare(
                    a.name,
                    b.name,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            for (int s = 0; s < sequences.Count; s++)
            {
                DialogueSequence sequence = sequences[s];

                if (sequence.lines == null || sequence.lines.Length == 0)
                    continue;

                report.AppendLine(
                    sequence.name + "  (" + sequence.lines.Length +
                    " line(s))"
                );

                bool changed = false;

                for (int i = 0; i < sequence.lines.Length; i++)
                {
                    DialogueLine line = sequence.lines[i];
                    string clipPath = FindClipPath(sequence.name, i + 1);
                    AudioClip clip = clipPath != null
                        ? AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath)
                        : null;

                    string label = "[" + (i + 1).ToString("00") + "]";

                    if (line == null)
                    {
                        missing++;
                        report.AppendLine(
                            label + " MISSING  (empty line entry)"
                        );

                        continue;
                    }

                    if (clip == null)
                    {
                        missing++;
                        report.AppendLine(
                            label + " MISSING  " +
                            Path.GetFileName(ClipName(sequence.name, i + 1))
                        );

                        continue;
                    }

                    if (line.voiceClip != null && !overwrite)
                    {
                        kept++;
                        report.AppendLine(
                            label + " KEPT     " + clipPath
                        );

                        continue;
                    }

                    line.voiceClip = clip;
                    assigned++;
                    changed = true;
                    report.AppendLine(
                        label + " ASSIGNED " + clipPath
                    );
                }

                if (changed)
                {
                    EditorUtility.SetDirty(sequence);
                    AssetDatabase.SaveAssetIfDirty(sequence);
                }

                report.AppendLine();
            }

            AssetDatabase.SaveAssets();
            DialogueDatabase.ClearCache();

            report.AppendLine(
                "Assigned: " + assigned + ", kept: " + kept +
                ", missing: " + missing
            );

            WriteReport(report.ToString());
            Debug.Log(
                "DialogueVoiceAssigner: assigned " + assigned +
                ", kept " + kept + ", missing " + missing + "."
            );
        }
        catch (Exception error)
        {
            report.AppendLine("ERROR: " + error);
            WriteReport(report.ToString());
            Debug.LogError("DialogueVoiceAssigner: " + error);
        }
    }

    private static string FindClipPath(string sequenceName, int lineNumber)
    {
        string baseName = ClipName(sequenceName, lineNumber);

        for (int i = 0; i < Extensions.Length; i++)
        {
            string path = VoiceFolder + "/" + baseName + Extensions[i];

            if (File.Exists(Path.Combine(ProjectRoot, path)))
                return path;
        }

        return null;
    }

    private static string ClipName(string sequenceName, int lineNumber)
    {
        return sequenceName + "_" + lineNumber.ToString("00");
    }

    private static void WriteReport(string report)
    {
        try
        {
            string logs = Path.Combine(ProjectRoot, "Logs");

            if (!Directory.Exists(logs))
                Directory.CreateDirectory(logs);

            File.WriteAllText(
                Path.Combine(logs, "dialogue_voice_assign.txt"),
                report
            );
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "DialogueVoiceAssigner: could not write report: " +
                error.Message
            );
        }
    }
}
#endif
