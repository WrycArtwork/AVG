using Godot;
using System;
using System.Collections.Generic;
using AVG.Scripts;

public partial class GallerySlotItem : TextureButton
{
    [Export] public TextureRect ThumbnailImage;
    [Export] public Texture2D LockedTexture;
    [Export] public Label CgsLabel;
    
    public event Action<CgGroupData, List<string>> OnClicked;
    private CgGroupData _groupData;
    private readonly List<string> _unlockedCgs = new();

    public void Setup(CgGroupData groupData)
    {
        _groupData = groupData;
        _unlockedCgs.Clear();

        if (groupData.CgsPaths != null)
        {
            foreach (var path in groupData.CgsPaths)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (GalleryManager.Instance != null && (GalleryManager.Instance.IsCgUnlocked(fileName) || GalleryManager.Instance.IsCgUnlocked(groupData.GroupId)))
                {
                    _unlockedCgs.Add(path);
                }
            }
        }
        
        bool isUnlocked = _unlockedCgs.Count > 0;
        Disabled = !isUnlocked;

        if (isUnlocked)
        {
            string thumbPath = !string.IsNullOrEmpty(groupData.ThumbnailPath) ? groupData.ThumbnailPath : _unlockedCgs[0];
            
            if (ThumbnailImage != null && FileAccess.FileExists(thumbPath))
            {
                ThumbnailImage.Texture = GD.Load<Texture2D>(thumbPath);
                ThumbnailImage.Modulate = Colors.White;
            }

            if (CgsLabel != null)
            {
                CgsLabel.Visible = true;
                CgsLabel.Text = $"{_unlockedCgs.Count}/{groupData.CgsPaths.Count}";
            }
        }
        else
        {
            if (ThumbnailImage != null)
            {
                ThumbnailImage.Texture = LockedTexture;
                ThumbnailImage.Modulate = LockedTexture != null ? Colors.White : new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
        }
        
        Pressed += OnClick;
    }

    private void OnClick()
    {
        if (_unlockedCgs.Count > 0)
        {
            OnClicked?.Invoke(_groupData, _unlockedCgs);
        }
    }
}
