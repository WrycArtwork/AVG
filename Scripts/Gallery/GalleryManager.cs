using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AVG.Scripts;

public partial class GalleryManager : Node
{
    public static GalleryManager Instance { get; private set; }
    private const string SavePath = "user://gallery_unlocks.json";

    private readonly HashSet<string> _unlockCgIds = new();

    [Export] public GalleryPanel GalleryPanelUI;

    public override void _Ready()
    {
        Instance = this;
        LoadUnlocks();
        if (GalleryPanelUI != null) GalleryPanelUI.Visible = false;
    }

    public void OpenGallery()
    {
        if (GalleryPanelUI != null) GalleryPanelUI.OpenPanel();
    }

    public void UnlockCg(string cgId)
    {
        if (string.IsNullOrEmpty(cgId)) return;

        if (_unlockCgIds.Add(cgId)) SaveUnlocks();

    }

    public bool IsCgUnlocked(string cgId)
    {
        return _unlockCgIds.Contains(cgId);
    }

    public void SaveUnlocks()
    {
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null) return;
        var json = JsonSerializer.Serialize(_unlockCgIds);
        file.StoreString(json);
    }

    public void LoadUnlocks()
    {
        if (!FileAccess.FileExists(SavePath)) return;
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null) return;
        var json = file.GetAsText();
        var list = JsonSerializer.Deserialize<HashSet<string>>(json);
        if (list != null)
        {
            _unlockCgIds.Clear();
            foreach (var id in list) _unlockCgIds.Add(id);
        }
    }
}
