using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Small, safe JSON persistence helper. Settings and story progress use
/// separate files, so starting a new game can never wipe preferences.
/// </summary>
public static class SaveIO
{
    public static string FolderPath =>
        Path.Combine(Application.persistentDataPath, "Paralysis");

    public static string SettingsPath =>
        Path.Combine(FolderPath, "settings.json");

    public static string ProgressPath =>
        Path.Combine(FolderPath, "progress.json");

    public static bool TryReadJson<T>(string path, out T value)
        where T : class
    {
        value = null;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        if (TryReadFile(path, out value))
            return true;

        // A half-written file should not brick the game; try the backup.
        return TryReadFile(path + ".bak", out value);
    }

    private static bool TryReadFile<T>(string path, out T value)
        where T : class
    {
        value = null;

        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(json))
                return false;

            value = JsonUtility.FromJson<T>(json);
            return value != null;
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "SaveIO: could not read '" + path + "': " + error.Message
            );

            return false;
        }
    }

    public static bool WriteJson<T>(string path, T value)
    {
        if (string.IsNullOrEmpty(path) || value == null)
            return false;

        try
        {
            string folder = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            string json = JsonUtility.ToJson(value, true);
            string temporary = path + ".tmp";

            File.WriteAllText(temporary, json);

            if (File.Exists(path))
            {
                try
                {
                    File.Copy(path, path + ".bak", true);
                }
                catch (Exception)
                {
                    // The backup is a convenience, not a requirement.
                }

                File.Delete(path);
            }

            File.Move(temporary, path);
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "SaveIO: could not write '" + path + "': " + error.Message
            );

            return false;
        }
    }

    public static void Delete(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "SaveIO: could not delete '" + path + "': " + error.Message
            );
        }
    }
}
