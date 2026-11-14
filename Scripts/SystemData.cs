using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace  AVG.Scripts;

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

    public void Reset()
    {
        TextSpeed = 0.05f;
        AutoDelayTime = 1.5f;
        MasterVolume = 1.0f;
        MusicVolume = 1.0f;
        SfxVolume = 1.0f;
        VoiceVolume = 1.0f;
        if (WindowModeIndex != 1) WindowModeIndex = 1;
        if (ResolutionIndex != 1) ResolutionIndex = 1;
    }
}

public class GalleryDatabase
{
    public List<CgGroupData> StoryCgs { get; set; } = new();
    public List<CgGroupData> BgCgs { get; set; } = new();
    public List<MusicTrackData> Musics { get; set; } = new();
}

public class CgGroupData
{
    public string GroupId {get; set;}
    public string GroupName {get; set;}
    public string ThumbnailPath {get; set;}
    public List<string> CgsPaths { get; set; } = new();
}

public class MusicTrackData
{
    public string TrackId {get; set;}
    public string TrackTitle {get; set;}
    public string AudioPath {get; set;}
}

public class SystemSaveModel
{
    public ConfigData Config { get; set; } = new();
    public HashSet<string> GalleryUnlocks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class SystemData
{
    private static SystemSaveModel _saveData;
    public static SystemSaveModel SaveData
    {
        get
        {
            if (_saveData == null) Load();
            return _saveData;
        }
    }
    
    public static ConfigData Config => _saveData.Config;
    public static HashSet<string> GalleryUnlocks => _saveData.GalleryUnlocks;

    public static void Save()
    {
        using var file = FileAccess.Open(GamePaths.SystemData, FileAccess.ModeFlags.Write);
        if (file == null) return;
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        file.StoreString(JsonSerializer.Serialize(SaveData, options));
    }

    public static void Load()
    {
        if (!FileAccess.FileExists(GamePaths.SystemData))
        {
            _saveData = new SystemSaveModel();
            _saveData.Config.Language = SteamLocaleAdapter.GetSteamLanguage("zh_TW");
            Save();
            return;
        }
        
        if (!FileAccess.FileExists(GamePaths.SystemData))
        {
            _saveData = new SystemSaveModel();
            return;
        }
        
        using var file = FileAccess.Open(GamePaths.SystemData, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            _saveData = new SystemSaveModel();
            return;
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _saveData = JsonSerializer.Deserialize<SystemSaveModel>(file.GetAsText(), options) ?? new SystemSaveModel();
            _saveData.GalleryUnlocks = new HashSet<string>(_saveData.GalleryUnlocks, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            _saveData = new SystemSaveModel();
        }
    }
}
