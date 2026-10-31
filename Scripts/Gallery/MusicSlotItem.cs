using Godot;
using System;
using AVG.Scripts;

public partial class MusicSlotItem : Button
{
    [Export] public Label TitleLabel;
    [Export] public TextureRect PlayingIcon;
    
    public event Action<MusicTrackData> OnTrackSlected;
    private MusicTrackData _trackData;
    
    public void Setup(MusicTrackData trackData, bool isCurrentPlaying = false)
    {
        _trackData = trackData;
        
        bool isUnlocked = GalleryManager.Instance != null && GalleryManager.Instance.IsCgUnlocked(trackData.TrackId);
        
        Disabled = !isUnlocked;

        if (TitleLabel != null)
        {
            TitleLabel.Text = isUnlocked ? trackData.TrackTitle : "???";
        }

        if (PlayingIcon != null)
        {
            PlayingIcon.Visible = isUnlocked && isCurrentPlaying;
        }
        
        if (isUnlocked)
        {
            Pressed += OnPressed;
        }
    }

    private void OnPressed()
    {
        OnTrackSlected?.Invoke(_trackData);
    }
}
