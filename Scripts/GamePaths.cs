using Godot;
using System;

namespace AVG.Scripts;

public static class GamePaths
{
    //__System & Save__
    public const string SaveDir = "user://Saves";
    public const string ThumbnailDir = $"{SaveDir}/Thumbnails";
    public const string SystemData = $"{SaveDir}/system_data.json";
    public static string SaveJson(int slot) => $"{SaveDir}/save_{slot:D2}.json";
    public static string Thumbnail(int slot) => $"{ThumbnailDir}/thumb_{slot:D2}.jpg";
    
    //__Data__
    public const string DataDir = $"res://Data";
    public const string ConditionsDataDir = $"{DataDir}/Conditions.json";
    public const string ChaptersMappingData =  $"{DataDir}/ChaptersMapping.json";
    public const string GalleryData = $"{DataDir}/GalleryData.json";
    public const string InputMappingData = $"{DataDir}/InputMapping.json";
    public static string DialogueData(string language) => $"{DataDir}/Dialogue/{language}/DialogueData.json";
    
    //__Scene__
    public const string SceneTitle = "res://Scenes/Title.tscn";
    public const string SceneDialogue = "res://Scenes/Dialogue.tscn";
    
    //__Assets__
    public const string AssetsDir = "res://Assets";
    public const string BackgroundDir = $"{AssetsDir}/Background";
    public const string CgDir = $"{AssetsDir}/CG";
    public const string CharacterDir = $"{AssetsDir}/Character";
    public const string AudioDir = $"{AssetsDir}/Audio";
    public const string BgmDir = $"{AudioDir}/BGM";
    public const string SfxDir = $"{AudioDir}/SFX";
    public const string VoiceDir = $"{AudioDir}/Voice";
    public const string IconDir = $"{AssetsDir}/UI/Icon"; 
    
    public static string GetValidAudio(string dir, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string basePath = $"{dir}/{fileName}";
        if (FileAccess.FileExists(basePath + ".ogg")) return basePath + ".ogg";
        if (FileAccess.FileExists(basePath + ".mp3")) return basePath + ".mp3";
        if (FileAccess.FileExists(basePath + ".wav")) return basePath + ".wav";
        return FileAccess.FileExists(basePath) ? basePath : null;
    }

    public static string GetValidImage(string dir, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string basePath = $"{dir}/{fileName}";
        if (FileAccess.FileExists(basePath + ".png")) return basePath + ".png";
        if (FileAccess.FileExists(basePath + ".jpg")) return basePath + ".jpg";
        if (FileAccess.FileExists(basePath + ".webp")) return basePath + ".webp";
        return FileAccess.FileExists(basePath) ? basePath : null;
    }
}
