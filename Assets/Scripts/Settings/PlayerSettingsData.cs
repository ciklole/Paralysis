using System;

/// <summary>Player preferences. This is deliberately separate from story progress.</summary>
[Serializable]
public class PlayerSettingsData
{
    public int version = 1;
    public float mouseSensitivity = 2f;
    public int frameRate = 60;
}
