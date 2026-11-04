using System;
using UnityEngine;

/// <summary>
/// One selectable chapter. Everything the chapter-selection screen and the
/// chapter-introduction screen need is configured here.
/// </summary>
[Serializable]
public class ChapterDefinition
{
    [Header("Identity")]
    public string chapterId = "chapter_1";
    public string chapterNumber = "1";
    public string chapterTitle = "THE BEGINNING";

    [TextArea(2, 4)]
    public string subtitle = "";

    [TextArea(2, 4)]
    public string description = "";

    [Header("Scene")]
    public string sceneName = "Home";

    [Tooltip("Lower numbers are shown first.")]
    public int displayOrder = 0;

    public bool enabled = true;

    [Header("Chapter Introduction")]
    public bool showChapterIntro = true;

    [Tooltip(
        "When enabled, this chapter uses the database's default intro style. " +
        "Turn it off to override fonts, positions and timings below.")]
    public bool useGlobalIntroStyle = true;

    public ChapterIntroStyle introStyle = new ChapterIntroStyle();
}
