using Godot;
using System;

namespace AVG.Scripts;

public partial class Title : Control
{
    [ExportGroup("Buttons")]
    [Export] public Button StartButton;
    [Export] public Button LoadButton;
    [Export] public Button ConfigButton;
    [Export] public Button GalleryButton;
    [Export] public Button ExitButton;

    public override void _Ready()
    {
        if (StartButton != null) StartButton.Pressed += OnStartPressed;
        if (ConfigButton != null)
        {
            ConfigButton.Pressed += () => ConfigManager.Instance?.OpenConfigPanel();
        }

        if (GalleryButton != null) GalleryButton.Pressed += () => GalleryManager.Instance?.OpenGallery();
        if (ExitButton != null) ExitButton.Pressed += OnExitPressed;
        
        StartButton?.GrabFocus();
        
        if (LoadButton != null)
        {
            LoadButton.Pressed += () => SaveLoadManager.Instance.OpenLoadMenu();
        }
    }

    public void OnStartPressed()
    {
        GetTree().ChangeSceneToFile(GamePaths.SceneDialogue);
    }

    public void OnExitPressed()
    {
        GetTree().Quit();
    }
}
