using Godot;
using System;

[Serializable]
public class ConfigData
{
    public float TextSpeed { get; set; } = 0.05f;
    public double AutoDelayTime { get; set; } = 1.5;
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 1.0f;
    public float SfxVolume { get; set; } =  1.0f;
    public float VoiceVolume { get; set; } = 1.0f;
    
    public int WindowModeIndex { get; set; } = 1;
    public int ResolutionIndex { get; set; } = 1;
    
    public string Language { get; set; } = "zh_TW";
}
