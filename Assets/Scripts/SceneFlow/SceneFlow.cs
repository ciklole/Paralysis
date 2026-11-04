using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The single entry point for menu-driven scene changes. Existing gameplay
/// teleports and car transitions keep using SceneManager directly; this class
/// only adds the loading screen, chapter introduction and menu flow around the
/// transitions that request it.
/// </summary>
[NoAutoStaticsCleanup]
public static class SceneFlow
{
    public static bool IsTransitioning { get; private set; }
    public static string LastError { get; private set; }
    public static SceneFlowConfig Config { get; private set; }

    public static string MainMenuScene
    {
        get
        {
            EnsureConfig();

            if (Config != null &&
                !string.IsNullOrEmpty(Config.mainMenuScene))
            {
                return Config.mainMenuScene;
            }

            return "MainMenu";
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        IsTransitioning = false;
        LastError = null;
        EnsureConfig();
    }

    private static void EnsureConfig()
    {
        if (Config == null)
            Config = Resources.Load<SceneFlowConfig>(
                "Paralysis/SceneFlowConfig"
            );
    }

    public static bool Go(TransitionRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.sceneName))
            return false;

        if (IsTransitioning)
        {
            Debug.LogWarning(
                "SceneFlow: a transition is already running; ignored '" +
                request.sceneName + "'."
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(request.sceneName))
        {
            LastError =
                "Scene '" + request.sceneName +
                "' is not in Build Settings.";

            Debug.LogError("SceneFlow: " + LastError);
            return false;
        }

        EnsureConfig();

        IsTransitioning = true;
        LastError = null;

        // Pause is abandoned without resuming gameplay; the loading screen
        // owns the screen from here.
        if (PauseManager.Instance != null)
            PauseManager.Instance.AbortForTransition();

        UIState.ClearModals();
        PlayerControl.ForceUnlockAll();
        CarDriveSession.Clear();

        if (request.reason == TransitionReason.MainMenu)
        {
            GameProgress.EndSessionForMenu();
            request.saveProgress = false;
        }
        else if (request.saveProgress && request.chapter != null)
        {
            // An explicit chapter choice may move backwards (chapter select),
            // so this is intentionally a forced save.
            GameProgress.ResumeSession(request.chapter);
        }
        else if (request.reason == TransitionReason.Continue)
        {
            GameProgress.ResumeSession(request.chapter);
        }

        SceneTransitionManager manager = SceneTransitionManager.Ensure();

        manager.StartTransition(request, (success, error) =>
        {
            IsTransitioning = false;
            LastError = error;

            if (!success && !string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    "SceneFlow: transition failed: " + error
                );
            }
        });

        return true;
    }

    public static bool GoToChapter(
        ChapterDefinition chapter,
        bool showChapterIntro,
        TransitionReason reason)
    {
        if (chapter == null || string.IsNullOrEmpty(chapter.sceneName))
            return false;

        if (!chapter.enabled)
        {
            Debug.LogWarning(
                "SceneFlow: chapter '" + chapter.chapterTitle +
                "' is disabled."
            );

            return false;
        }

        TransitionRequest request = new TransitionRequest
        {
            sceneName = chapter.sceneName,
            chapter = chapter,
            showChapterIntro =
                showChapterIntro && chapter.showChapterIntro,
            reason = reason,
            saveProgress = true
        };

        return Go(request);
    }

    public static bool NewGame(
        ChapterDefinition firstChapter,
        bool showChapterIntro)
    {
        if (firstChapter == null)
        {
            Debug.LogError(
                "SceneFlow: no first chapter is configured in the " +
                "ChapterDatabase."
            );

            return false;
        }

        GameProgress.BeginNewGame(firstChapter);

        return GoToChapter(
            firstChapter,
            showChapterIntro,
            TransitionReason.NewGame
        );
    }

    public static bool Continue(bool showChapterIntro)
    {
        if (!GameProgress.HasProgress)
            return false;

        ChapterDefinition chapter = GameProgress.CurrentChapter;
        string scene = GameProgress.CurrentScene;

        if (chapter != null)
            scene = chapter.sceneName;

        if (string.IsNullOrEmpty(scene) ||
            !Application.CanStreamedLevelBeLoaded(scene))
        {
            LastError = "The saved scene is no longer available.";
            Debug.LogWarning("SceneFlow: " + LastError);
            return false;
        }

        GameProgress.ResumeSession(chapter);

        return Go(new TransitionRequest
        {
            sceneName = scene,
            chapter = chapter,
            showChapterIntro =
                showChapterIntro &&
                chapter != null &&
                chapter.showChapterIntro,
            reason = TransitionReason.Continue,
            saveProgress = chapter != null
        });
    }

    public static bool ReplayCurrent(bool showChapterIntro)
    {
        string activeScene = SceneManager.GetActiveScene().name;
        ChapterDatabase database = ChapterDatabase.Load();

        ChapterDefinition chapter = database != null
            ? database.FindByScene(activeScene)
            : null;

        // A scene without a chapter entry (the driving interlude) simply
        // reloads itself. Falling back to the last chapter would silently
        // teleport the player somewhere else.
        if (chapter == null)
        {
            return Go(new TransitionRequest
            {
                sceneName = activeScene,
                chapter = null,
                showChapterIntro = false,
                reason = TransitionReason.Replay,
                saveProgress = false
            });
        }

        GameProgress.ResumeSession(chapter);

        return GoToChapter(
            chapter,
            showChapterIntro,
            TransitionReason.Replay
        );
    }

    public static bool GoToMainMenu()
    {
        return Go(new TransitionRequest
        {
            sceneName = MainMenuScene,
            chapter = null,
            showChapterIntro = false,
            reason = TransitionReason.MainMenu,
            saveProgress = false
        });
    }

    /// <summary>Called by the transition manager once the new scene is active.</summary>
    public static void NotifySceneActivated(string sceneName)
    {
        if (sceneName == MainMenuScene)
            GameSession.CleanupForMenu();
    }
}
