#if UNITY_EDITOR
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batch-mode entry point for the play-mode smoke checks. Pair it with the
/// "-paralysisSmoke" argument so PlayModeSmokeTest boots in the player loop.
/// </summary>
[NoAutoStaticsCleanup]
public static class SmokeTestRunner
{
    public static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "SmokeTestRunner: the editor is already in Play Mode."
            );

            return;
        }

        Debug.Log("SmokeTestRunner: entering Play Mode.");

        EditorApplication.delayCall += EnterPlayMode;
    }

    private static void EnterPlayMode()
    {
        EditorApplication.EnterPlaymode();
    }
}
#endif
