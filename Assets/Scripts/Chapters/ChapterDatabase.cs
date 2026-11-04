using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

public enum ChapterAvailabilityMode
{
    [Tooltip("Development: every configured chapter can be selected.")]
    Development_AllChapters,

    [Tooltip("Player build: chapters unlock through saved story progress.")]
    Player_Progression
}

/// <summary>
/// Data-driven chapter list. Edit it in the Inspector, add or remove entries,
/// and the main menu and chapter-introduction screen follow automatically.
/// </summary>
[CreateAssetMenu(
    fileName = "ChapterDatabase",
    menuName = "Paralysis/Chapter Database")]
[NoAutoStaticsCleanup]
public class ChapterDatabase : ScriptableObject
{
    private const string ResourcePath = "Paralysis/ChapterDatabase";

    public ChapterAvailabilityMode availabilityMode =
        ChapterAvailabilityMode.Development_AllChapters;

    public ChapterIntroStyle defaultIntroStyle = new ChapterIntroStyle();

    public List<ChapterDefinition> chapters =
        new List<ChapterDefinition>();

    private static ChapterDatabase cached;
    private static bool lookedUp;

    public static ChapterDatabase Load()
    {
        if (!lookedUp)
        {
            lookedUp = true;
            cached = Resources.Load<ChapterDatabase>(ResourcePath);
        }

        return cached;
    }

    /// <summary>Used by editor tooling after the asset has been created.</summary>
    public static void ClearCache()
    {
        cached = null;
        lookedUp = false;
    }

    public List<ChapterDefinition> GetOrderedChapters(
        bool includeDisabled = false)
    {
        List<ChapterDefinition> result =
            new List<ChapterDefinition>(chapters.Count);

        for (int i = 0; i < chapters.Count; i++)
        {
            ChapterDefinition chapter = chapters[i];

            if (chapter == null)
                continue;

            if (!includeDisabled && !chapter.enabled)
                continue;

            result.Add(chapter);
        }

        // Stable order: displayOrder first, then list order.
        result.Sort((a, b) =>
        {
            int order = a.displayOrder.CompareTo(b.displayOrder);

            if (order != 0)
                return order;

            return chapters.IndexOf(a).CompareTo(chapters.IndexOf(b));
        });

        return result;
    }

    public ChapterDefinition FirstChapter
    {
        get
        {
            List<ChapterDefinition> ordered = GetOrderedChapters();
            return ordered.Count > 0 ? ordered[0] : null;
        }
    }

    public ChapterDefinition FindById(string chapterId)
    {
        if (string.IsNullOrEmpty(chapterId))
            return null;

        for (int i = 0; i < chapters.Count; i++)
        {
            ChapterDefinition chapter = chapters[i];

            if (chapter != null &&
                string.Equals(
                    chapter.chapterId,
                    chapterId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return chapter;
            }
        }

        return null;
    }

    public ChapterDefinition FindByScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return null;

        for (int i = 0; i < chapters.Count; i++)
        {
            ChapterDefinition chapter = chapters[i];

            if (chapter != null &&
                string.Equals(
                    chapter.sceneName,
                    sceneName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return chapter;
            }
        }

        return null;
    }

    public int IndexOf(ChapterDefinition chapter)
    {
        if (chapter == null)
            return -1;

        return GetOrderedChapters(true).IndexOf(chapter);
    }

    public ChapterDefinition NextChapter(ChapterDefinition chapter)
    {
        List<ChapterDefinition> ordered = GetOrderedChapters();
        int index = ordered.IndexOf(chapter);

        if (index < 0 || index + 1 >= ordered.Count)
            return null;

        return ordered[index + 1];
    }

    private void OnValidate()
    {
        if (chapters == null)
            chapters = new List<ChapterDefinition>();
    }
}

/// <summary>
/// Central availability rule. Development mode unlocks everything for
/// testing; player mode uses the saved unlock list and always keeps the first
/// chapter accessible.
/// </summary>
public static class ChapterAvailability
{
    public static bool IsAvailable(
        ChapterDefinition chapter,
        ChapterDatabase database)
    {
        if (chapter == null || !chapter.enabled)
            return false;

        if (database == null)
            return true;

        if (database.availabilityMode ==
            ChapterAvailabilityMode.Development_AllChapters)
        {
            return true;
        }

        if (database.IndexOf(chapter) == 0)
            return true;

        return GameProgress.IsChapterUnlocked(chapter.chapterId);
    }
}
