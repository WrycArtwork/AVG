using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AVG.Scripts;

public partial class GalleryManager : Node
{
    public static GalleryManager Instance { get; private set; }

    [Export] public GalleryPanel GalleryPanelUI;

    public override void _Ready()
    {
        Instance = this;
        if (GalleryPanelUI != null) GalleryPanelUI.Visible = false;
    }

    public void OpenGallery()
    {
        if (GalleryPanelUI != null) GalleryPanelUI.OpenPanel();
    }

    public void UnlockCg(string cgId)
    {
        if (string.IsNullOrEmpty(cgId)) return;

        if (SystemData.GalleryUnlocks.Add(cgId.Trim()))
        {
            SystemData.Save();
        }

    }

    public bool IsCgUnlocked(string cgId)
    {
        if (string.IsNullOrWhiteSpace(cgId)) return false;
        return SystemData.GalleryUnlocks.Contains(cgId.Trim());
    }
}
