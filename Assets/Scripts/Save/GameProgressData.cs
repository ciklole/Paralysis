using System;
using System.Collections.Generic;

/// <summary>Saved story position. Settings live in a separate file.</summary>
[Serializable]
public class GameProgressData
{
    public int version = 1;
    public string currentChapterId = "";
    public string currentScene = "";
    public int highestChapterIndex = -1;
    public List<string> unlockedChapterIds = new List<string>();
    public string savedAtUtc = "";
}
