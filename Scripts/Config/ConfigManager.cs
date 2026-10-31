using Godot;
using System;
using System.Text.Json;

namespace AVG.Scripts;
public partial class ConfigManager : Node
{
    public static ConfigManager Instance { get; private set; }
    public const string ConfigPath = "user://config.json";

    public ConfigData CurrentConfig { get; private set; } = new();

    [Export] public ConfigPanel ConfigPanelUI;
    
    public event Action<ConfigData> OnConfigChanged;
    public event Action<string> OnLanguageChanged; 
    public event Action ConfigPanelClose;

    public static readonly Vector2I[] Resolutions = new[]
    {
        new Vector2I(1280, 720),
        new Vector2I(1366, 768),
        new Vector2I(1600, 900),
        new Vector2I(1920, 1080),
        new Vector2I(2560, 1440),
    };

    public override void _Ready()
    {
        Instance = this;
        LoadConfig();
        ApplyAllSettings();

        if (ConfigPanelUI != null)
        {
            ConfigPanelUI.Visible = false;
            ConfigPanelUI.ConfigPanelClose += () =>
            {
                SaveConfig();
                ConfigPanelClose?.Invoke();
            };
        }

        
    }

    public void OpenConfigPanel()
    {
        if (ConfigPanelUI != null)
        {
            ConfigPanelUI.OpenPanel(CurrentConfig);
        }
    }

    public void ApplyAllSettings()
    {
        SetBusVolume("Master", CurrentConfig.MasterVolume);
        SetBusVolume("Music", CurrentConfig.MusicVolume);
        SetBusVolume("Sfx", CurrentConfig.SfxVolume);
        SetBusVolume("Voice", CurrentConfig.VoiceVolume);
        
        ApplyWindowMode(CurrentConfig.WindowModeIndex);

        if (CurrentConfig.WindowModeIndex == 0)
        {
            ApplyResolution(CurrentConfig.ResolutionIndex);
        }
        
        TranslationServer.SetLocale(CurrentConfig.Language);
        
        OnConfigChanged?.Invoke(CurrentConfig);
    }

    public void SetBusVolume(string busName, float volume)
    {
        int busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex != -1)
        {
            float db = volume > 0.0001f ? Mathf.LinearToDb(volume) : -80.0f;
            AudioServer.SetBusVolumeDb(busIndex, db);
            AudioServer.SetBusMute(busIndex, volume <= 0.0001f);
        }
    }

    public void ApplyWindowMode(int modeIndex)
    {
        switch (modeIndex)
        {
            case 0:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                break;
            case 1:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
                break;
        }
        
        ApplyResolution(CurrentConfig.ResolutionIndex);
    }

    public void ApplyResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= Resolutions.Length) return;
        
        CurrentConfig.ResolutionIndex = resolutionIndex;
        var targetSize = Resolutions[resolutionIndex];

        if (CurrentConfig.WindowModeIndex == 0)
        {
            DisplayServer.WindowSetSize(targetSize, 0);
            
            int currentScreen = DisplayServer.WindowGetCurrentScreen();
            Vector2I screenSize =  DisplayServer.ScreenGetSize(currentScreen);
            Vector2I screenPosition =  DisplayServer.ScreenGetPosition(currentScreen);
            Vector2I centerPosition = screenPosition + (screenSize - targetSize)/2;
            DisplayServer.WindowSetPosition(centerPosition, 0);
        }
    }

    public void ApplyLanguage(string language)
    {
        CurrentConfig.Language = language;
        TranslationServer.SetLocale(language);
        OnLanguageChanged?.Invoke(language);
    }

    public void SaveConfig()
    {
        using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
        if (file == null) return;
        var json = JsonSerializer.Serialize(CurrentConfig, new JsonSerializerOptions { WriteIndented = true });
        file.StoreString(json);
    }

    private void LoadConfig()
    {
        if (!FileAccess.FileExists(ConfigPath)) return;
        using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null) return;
        var json = file.GetAsText();
        var data = JsonSerializer.Deserialize<ConfigData>(json);
        if (data != null) CurrentConfig = data;
    }
}
