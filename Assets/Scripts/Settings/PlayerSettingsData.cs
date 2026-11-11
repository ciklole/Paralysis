using System;

/// <summary>Player preferences. This is deliberately separate from story progress.</summary>
[Serializable]
public class PlayerSettingsData
{
    public int version = 2;
    public float mouseSensitivity = 2f;
    public int frameRate = 60;
    public bool vsync = false;
    public bool motionBlur = true;

    // Zero means "use the resolution the game started with". A saved pair is
    // only applied when the current display still reports it as supported.
    public int resolutionWidth = 0;
    public int resolutionHeight = 0;
}
