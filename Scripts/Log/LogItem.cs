using Godot;
using System;

public partial class LogItem : Control
{
    [Export] public PanelContainer DialoguePanel;
    [Export] public Label NameLabel;
    [Export] public RichTextLabel ContentLabel;
    [Export] public Button VoiceReplayButton;
    
    [Export] public PanelContainer OptionBoxPanel;
    [Export] public RichTextLabel OptionsLabel;

    private string _voicePath;
    
    public static event Action<string> PlayVoice; 

    public void Setup(string name, string content, string voicePath = null, bool isOption =  false)
    {
        if (isOption)
        {
            if (DialoguePanel != null) DialoguePanel.Visible = false;
            if (OptionBoxPanel != null) OptionBoxPanel.Visible = true;
            if (OptionsLabel != null) OptionsLabel.Text = content;
            return;
        }
        else
        {
            if (DialoguePanel != null) DialoguePanel.Visible = true;
            if (OptionBoxPanel != null) OptionBoxPanel.Visible = false;
            
            if (NameLabel != null)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    NameLabel.Visible = false;
                }
                else
                {
                    NameLabel.Visible = true;
                    NameLabel.Text = name;
                }
            }

            if (ContentLabel != null)
            {
                ContentLabel.Text = content;
            }

            _voicePath = voicePath;

            if (VoiceReplayButton != null)
            {
                VoiceReplayButton.Visible = !string.IsNullOrWhiteSpace(_voicePath);
                VoiceReplayButton.Pressed += OnReplayVoicePressed;
            }
            
        }
    }

    private void OnReplayVoicePressed()
    {
        if (string.IsNullOrEmpty(_voicePath)) return;
        PlayVoice?.Invoke(_voicePath);
    }
}