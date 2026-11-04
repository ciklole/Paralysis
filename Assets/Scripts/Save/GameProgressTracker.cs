using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Survives scene loads and records which configured chapter the player
/// reached. Direct scene loads (the existing car/store flow) are tracked too.
/// </summary>
public class GameProgressTracker : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Ensure()
    {
        if (FindAnyObjectByType<GameProgressTracker>() != null)
            return;

        GameObject go = new GameObject("GameProgressTracker");
        DontDestroyOnLoad(go);
        go.AddComponent<GameProgressTracker>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        GameProgress.NotifySceneLoaded(scene.name);
    }
}
