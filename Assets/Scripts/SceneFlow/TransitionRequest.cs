using UnityEngine;

public enum TransitionReason
{
    NewGame,
    Continue,
    ChapterSelect,
    MainMenu,
    Replay,
    Direct
}

/// <summary>One requested scene transition.</summary>
public class TransitionRequest
{
    public string sceneName;
    public ChapterDefinition chapter;
    public bool showChapterIntro;
    public TransitionReason reason = TransitionReason.Direct;
    public bool saveProgress = true;
}
