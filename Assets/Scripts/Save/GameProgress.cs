using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

/// <summary>
/// Minimal story progress: the chapter/scene the player reached and the
/// chapters unlocked for a future player build. There are deliberately no
/// checkpoints, deaths or respawn state here.
/// </summary>
[NoAutoStaticsCleanup]
public static class GameProgress
{
    public static GameProgressData Data { get; private set; } =
        new GameProgressData();

    /// <summary>True only for the current play session, not across restarts.</summary>
    public static bool SessionActive { get; private set; }

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Reload();
    }

    public static void Reload()
    {
        GameProgressData loaded;

        if (SaveIO.TryReadJson(SaveIO.ProgressPath, out loaded))
            Data = loaded;
        else
            Data = new GameProgressData();

        Sanitize();
        SessionActive = false;
    }

    public static bool HasProgress
    {
        get
        {
            if (Data == null ||
                string.IsNullOrEmpty(Data.currentScene) ||
                Data.currentScene == SceneFlow.MainMenuScene)
            {
                return false;
            }

            return Application.CanStreamedLevelBeLoaded(Data.currentScene);
        }
    }

    public static string CurrentScene =>
        Data != null ? Data.currentScene : string.Empty;

    public static string CurrentChapterId =>
        Data != null ? Data.currentChapterId : string.Empty;

    public static ChapterDefinition CurrentChapter
    {
        get
        {
            ChapterDatabase database = ChapterDatabase.Load();

            if (database == null)
                return null;

            ChapterDefinition chapter = database.FindById(
                CurrentChapterId
            );

            if (chapter != null)
                return chapter;

            return database.FindByScene(CurrentScene);
        }
    }

    public static void Save()
    {
        if (Data == null)
            Data = new GameProgressData();

        Data.savedAtUtc = DateTime.UtcNow.ToString("o");
        SaveIO.WriteJson(SaveIO.ProgressPath, Data);
        Changed?.Invoke();
    }

    /// <summary>
    /// Starts a fresh story without touching the separate settings file.
    /// </summary>
    public static void BeginNewGame(ChapterDefinition firstChapter)
    {
        Data = new GameProgressData();
        SessionActive = true;

        if (firstChapter != null)
            SetCurrentChapter(firstChapter, true);
        else
            Save();
    }

    public static void ResumeSession(ChapterDefinition chapter)
    {
        SessionActive = true;

        if (chapter != null)
            SetCurrentChapter(chapter, true);
    }

    public static void EndSessionForMenu()
    {
        SessionActive = false;
    }

    public static void Clear()
    {
        Data = new GameProgressData();
        SessionActive = false;
        SaveIO.Delete(SaveIO.ProgressPath);
        Changed?.Invoke();
    }

    /// <summary>
    /// Stores the current chapter. Automatic tracking only moves forward, so
    /// walking back into the city cannot erase a later store chapter. Explicit
    /// chapter selection passes force = true.
    /// </summary>
    public static void SetCurrentChapter(
        ChapterDefinition chapter,
        bool force)
    {
        if (chapter == null)
            return;

        ChapterDatabase database = ChapterDatabase.Load();
        int index = database != null ? database.IndexOf(chapter) : -1;

        if (!force && database != null)
        {
            int currentIndex = database.IndexOf(
                database.FindById(CurrentChapterId)
            );

            if (currentIndex >= 0 && index >= 0 && index < currentIndex)
                return;
        }

        Data.currentChapterId = chapter.chapterId;
        Data.currentScene = chapter.sceneName;
        Data.highestChapterIndex = Mathf.Max(
            Data.highestChapterIndex,
            index
        );

        UnlockThrough(index, chapter.chapterId);
        Save();
    }

    /// <summary>Called by the tracker after every scene load.</summary>
    public static void NotifySceneLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        if (sceneName == SceneFlow.MainMenuScene)
        {
            SessionActive = false;
            return;
        }

        if (!SessionActive)
            return;

        ChapterDatabase database = ChapterDatabase.Load();

        if (database == null)
            return;

        ChapterDefinition chapter = database.FindByScene(sceneName);

        if (chapter != null)
            SetCurrentChapter(chapter, false);
    }

    public static bool IsChapterUnlocked(string chapterId)
    {
        if (string.IsNullOrEmpty(chapterId) || Data == null)
            return false;

        if (Data.unlockedChapterIds == null)
            return false;

        return Data.unlockedChapterIds.Contains(chapterId);
    }

    public static void UnlockChapter(string chapterId)
    {
        if (string.IsNullOrEmpty(chapterId) || Data == null)
            return;

        if (Data.unlockedChapterIds == null)
            Data.unlockedChapterIds = new List<string>();

        if (!Data.unlockedChapterIds.Contains(chapterId))
        {
            Data.unlockedChapterIds.Add(chapterId);
            Save();
        }
    }

    /// <summary>
    /// Future player-mode hook. Completing a chapter unlocks the next one.
    /// No gameplay code calls this yet.
    /// </summary>
    public static void MarkChapterCompleted(string chapterId)
    {
        ChapterDatabase database = ChapterDatabase.Load();

        if (database == null)
            return;

        ChapterDefinition chapter = database.FindById(chapterId);

        if (chapter == null)
            return;

        UnlockChapter(chapter.chapterId);

        ChapterDefinition next = database.NextChapter(chapter);

        if (next != null)
            UnlockChapter(next.chapterId);
    }

    private static void UnlockThrough(int index, string fallbackId)
    {
        if (Data.unlockedChapterIds == null)
            Data.unlockedChapterIds = new List<string>();

        ChapterDatabase database = ChapterDatabase.Load();

        if (database != null && index >= 0)
        {
            List<ChapterDefinition> ordered =
                database.GetOrderedChapters(true);

            for (int i = 0; i <= index && i < ordered.Count; i++)
            {
                ChapterDefinition chapter = ordered[i];

                if (chapter != null &&
                    !string.IsNullOrEmpty(chapter.chapterId) &&
                    !Data.unlockedChapterIds.Contains(chapter.chapterId))
                {
                    Data.unlockedChapterIds.Add(chapter.chapterId);
                }
            }
        }

        if (!string.IsNullOrEmpty(fallbackId) &&
            !Data.unlockedChapterIds.Contains(fallbackId))
        {
            Data.unlockedChapterIds.Add(fallbackId);
        }
    }

    private static void Sanitize()
    {
        if (Data == null)
            Data = new GameProgressData();

        if (Data.unlockedChapterIds == null)
            Data.unlockedChapterIds = new List<string>();

        Data.unlockedChapterIds.RemoveAll(string.IsNullOrEmpty);

        if (Data.highestChapterIndex < -1)
            Data.highestChapterIndex = -1;
    }
}

