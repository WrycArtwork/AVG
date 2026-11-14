using Godot;
using System;
using AVG.Scripts;

public partial class ConfigPanel : Control
{
    [ExportGroup("Text")]
    [Export] public HSlider TextSpeedSlider;
    [Export] public RichTextLabel TextSpeedPreviewLabel;
    [Export] public HSlider AutoDelaySlider;
    
    [ExportGroup("Audio")]
    [Export] public HSlider MasterVolumeSlider;
    [Export] public HSlider MusicVolumeSlider;
    [Export] public HSlider SfxVolumeSlider;
    [Export] public HSlider VoiceVolumeSlider;

    [ExportGroup("Display")]
    [Export] public OptionButton WindowModeOptionButton;
    [Export] public OptionButton ResolutionOptionButton;
    [Export] public OptionButton LanguageOptionButton;
    
    [ExportGroup("Buttons")]
    [Export] public Button CloseButton;
    [Export] public Button ResetButton;
    [Export] public Button TitleButton;

    public event Action ConfigPanelClose;
    public event Action ReturnToTitle;
    
    private ConfigData _configData;
    private Tween _previewTween;
    private const string PreviewSampleText = "文本顯示速度預覽。";
    
    private const float MinTypeSpeed = 0.1f;
    private const float MaxTypeSpeed = 0.005f;
    private const double MinAutoDelay = 0.1;
    private const double MaxAutoDelay = 1;

    private static readonly (string Name, string Language)[] SupportedLanguages = new[]
    {
        ("English", "en"),
        ("日本語", "ja"),
        ("繁體中文", "zh_TW"),
        ("简体中文", "zh_CN"),
    };

    public override void _Ready()
    {
        SetupOptionButton();

        if (CloseButton != null) CloseButton.Pressed += ClosePanel;
        if (ResetButton != null) ResetButton.Pressed += OnResetPressed;
        if (TitleButton != null) TitleButton.Pressed += OnReturnPressed;
        
        if (TextSpeedSlider != null)
        {
            TextSpeedSlider.ValueChanged += (val) =>
            {
                if (_configData == null) return;
                float time = (float)val / 100;
                _configData.TextSpeed = Mathf.Lerp(MinTypeSpeed, MaxTypeSpeed, time);
                PlayTextPreview();
            };
        }

        if (AutoDelaySlider != null)
        {
            AutoDelaySlider.ValueChanged += (val) =>
            {
                if (_configData == null) return;
                double time = val / 100;
                _configData.AutoDelayTime = Mathf.Lerp(MinAutoDelay, MaxAutoDelay, time);
            };
        }
        
        if (MasterVolumeSlider != null) MasterVolumeSlider.ValueChanged += (val) =>
        {
            float volume = (float)val / 100f;
            _configData.MasterVolume = volume;
            ConfigManager.Instance.SetBusVolume("Master", volume);
        };
        if (MusicVolumeSlider != null) MusicVolumeSlider.ValueChanged += (val) =>
        {
            float volume = (float)val / 100f;
            _configData.MusicVolume = volume; 
            ConfigManager.Instance.SetBusVolume("Music", volume);
        };
        if (SfxVolumeSlider != null) SfxVolumeSlider.ValueChanged += (val) =>
        {
            float volume = (float)val / 100f;
            _configData.SfxVolume = volume;
            ConfigManager.Instance.SetBusVolume("Sfx", volume);
        };
        if (VoiceVolumeSlider != null) VoiceVolumeSlider.ValueChanged += (val) =>
        {
            float volume = (float)val / 100f;
            _configData.VoiceVolume = volume;
            ConfigManager.Instance.SetBusVolume("Voice", volume);
        };

        if (WindowModeOptionButton != null)
        {
            WindowModeOptionButton.ItemSelected += (idx) =>
            {
                _configData.WindowModeIndex = (int)idx;
                ConfigManager.Instance.ApplyWindowMode((int)idx);
            };
        }

        if (ResolutionOptionButton != null)
        {
            ResolutionOptionButton.ItemSelected += (idx) =>
            {
                _configData.ResolutionIndex = (int)idx;
                ConfigManager.Instance.ApplyResolution((int)idx);
            };
        }

        if (LanguageOptionButton != null)
        {
            LanguageOptionButton.ItemSelected += (idx) =>
            {
                if (idx >= 0 && idx < SupportedLanguages.Length)
                {
                    string targetLanguage = SupportedLanguages[idx].Language;
                    _configData.Language = targetLanguage;

                    TranslationServer.SetLocale(targetLanguage);
                    ConfigManager.Instance.ApplyLanguage(targetLanguage);
                }
            };
        }
    }

    private void SetupSliders()
    {
        HSlider[] allSliders = { TextSpeedSlider, AutoDelaySlider, MasterVolumeSlider, MusicVolumeSlider, SfxVolumeSlider ,VoiceVolumeSlider};
        foreach (var slider in allSliders)
        {
            if (slider == null) continue;
            slider.MinValue = 0;
            slider.MaxValue = 100;
            slider.Step = 1;
        }
    }

    public void OpenPanel(ConfigData configData, bool canReturnToTitle = true)
    {
        _configData = configData;
        SyncUIWithData();
        Visible = true;
        PlayTextPreview();

        if (TitleButton != null)
        {
            TitleButton.Visible = canReturnToTitle;
        }
    }

    public void ClosePanel()
    {
        Visible = false;
        _previewTween?.Kill();
        ConfigPanelClose?.Invoke();
    }

    private void OnResetPressed()
    {
        if (_configData == null) return;
        _configData.Reset();
        SyncUIWithData();
        ConfigManager.Instance.ApplyAllSettings();
        PlayTextPreview();
    }

    private void OnReturnPressed()
    {
        ClosePanel();
        ReturnToTitle?.Invoke();
    }

    private void SyncUIWithData()
    {
        if (_configData == null) return;

        if (TextSpeedSlider != null)
        {
            float time = (_configData.TextSpeed - MinTypeSpeed) / (MaxTypeSpeed - MinTypeSpeed);
            TextSpeedSlider.Value = Mathf.Clamp(Mathf.Round(time * 100f), 0, 100);
        }

        if (AutoDelaySlider != null)
        {
            double time = (_configData.AutoDelayTime - MinAutoDelay) / (MaxAutoDelay - MinAutoDelay);
            AutoDelaySlider.Value = Mathf.Clamp(Mathf.Round(time * 100.0), 0, 100);
        }
        
        if (MasterVolumeSlider != null) MasterVolumeSlider.Value = Mathf.Round(_configData.MasterVolume * 100f);
        if (MusicVolumeSlider != null) MusicVolumeSlider.Value = Mathf.Round(_configData.MusicVolume * 100f);
        if (SfxVolumeSlider != null) SfxVolumeSlider.Value = Mathf.Round(_configData.SfxVolume * 100f);
        if (VoiceVolumeSlider != null) VoiceVolumeSlider.Value = Mathf.Round(_configData.VoiceVolume * 100f);
        if (WindowModeOptionButton != null) WindowModeOptionButton.Selected = _configData.WindowModeIndex;
        if (ResolutionOptionButton != null) ResolutionOptionButton.Selected = _configData.ResolutionIndex;
        if (LanguageOptionButton != null)
        {
            int languageIndex = Array.FindIndex(SupportedLanguages, language => language.Language == _configData.Language);
            LanguageOptionButton.Selected = languageIndex != -1 ? languageIndex : 0;
        }
    }

    private void PlayTextPreview()
    {
        if (TextSpeedPreviewLabel == null) return;
        
        _previewTween?.Kill();
        TextSpeedPreviewLabel.Text = PreviewSampleText;
        
        float duration = PreviewSampleText.Length * _configData.TextSpeed;
        _previewTween = CreateTween();
        
        _previewTween.TweenCallback(Callable.From(() =>
        {
            return TextSpeedPreviewLabel.VisibleRatio = 0f;
        }));
        _previewTween.TweenProperty(TextSpeedPreviewLabel, "visible_ratio", 1.0f, duration);

        _previewTween.TweenInterval(1.0f);
        _previewTween.SetLoops();
    }

    private void SetupOptionButton()
    {
        if (WindowModeOptionButton != null && WindowModeOptionButton.ItemCount == 0)
        {
            WindowModeOptionButton.AddItem("視窗", 0);
            WindowModeOptionButton.AddItem("全螢幕", 1);
        }

        if (ResolutionOptionButton != null && ResolutionOptionButton.ItemCount == 0)
        {
            ResolutionOptionButton.AddItem("1280, 720", 0);
            ResolutionOptionButton.AddItem("1366, 768", 1);
            ResolutionOptionButton.AddItem("1600, 900", 2);
            ResolutionOptionButton.AddItem("1920, 1080", 3);
            ResolutionOptionButton.AddItem("2560, 1440", 4);
        }

        if (LanguageOptionButton != null && LanguageOptionButton.ItemCount == 0)
        {
            for (int i = 0; i < SupportedLanguages.Length; i++)
            {
                LanguageOptionButton.AddItem(SupportedLanguages[i].Language, i);
            }
        }
    }

}
