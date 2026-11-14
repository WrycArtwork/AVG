using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using AVG.Scripts;

public partial class GalleryPanel : Control
{
    [Export] public int SlotsPerPage = 8;
    [Export] public Label PageLabel;
    [Export] public AudioStreamPlayer MusicPlayer;
    
    [ExportGroup("Buttons")]
    [Export] public Button StoryCgTabButton;
    [Export] public Button BgCgTabButton;
    [Export] public Button MusicTabButton;
    [Export] public Button CloseButton;
    [Export] public Button PrevPageButton;
    [Export] public Button NextPageButton;
    
    [ExportGroup("Containers")]
    [Export] public GridContainer StoryGrid;
    [Export] public GridContainer BgGrid;
    [Export] public VBoxContainer MusicContainer;
    
    [ExportGroup("Prefabs")]
    [Export] public PackedScene SlotItemPrefab;
    [Export] public PackedScene MusicItemPrefab;
    [Export] public GalleryPreviewPanel PreviewPanel;

    private List<CgGroupData> _storyCgList = new();
    private List<CgGroupData> _bgCgList = new();
    private List<MusicTrackData> _musicList = new();
    
    private int _currentTab = 0;
    private int _currentPage = 0;
    private string _currentPlayingTrackedId = "";

    public override void _Ready()
    {

        Visible = false;

        if (StoryCgTabButton != null) StoryCgTabButton.Pressed += () => SwitchTab(0);
        if (BgCgTabButton != null) BgCgTabButton.Pressed += () => SwitchTab(1);
        if (MusicTabButton != null) MusicTabButton.Pressed += () => SwitchTab(2);
        if (PrevPageButton != null) PrevPageButton.Pressed += OnPrevPressed;
        if (NextPageButton != null) NextPageButton.Pressed += OnNextPressed;

        if (CloseButton != null)
        {
            CloseButton.Pressed += () =>
            {
                MusicPlayer?.Stop();
                Visible = false;
            };
        }

        if (MusicPlayer != null)
        {
            MusicPlayer.Bus = "Music";
        }

        LoadGalleryData();
    }

    public void OpenPanel()
    {
        LoadGalleryData();
        Visible = true;
        SwitchTab(0);
    }

    private void LoadGalleryData()
    {
        const string path = GamePaths.GalleryData;
        if (!FileAccess.FileExists(path)) return;
        
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var jsonText = file.GetAsText();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<GalleryDatabase>(jsonText, options);

        if (data != null)
        {
            _storyCgList = data.StoryCgs ?? new();
            _bgCgList = data.BgCgs ?? new();
            _musicList = data.Musics ?? new();
        }
    }

    private void SwitchTab(int index)
    {
        _currentTab = index;
        _currentPage = 0;
        
        if (StoryGrid != null) StoryGrid.Visible = (index == 0);
        if (BgGrid != null) BgGrid.Visible = (index == 1);
        if (MusicContainer != null) MusicContainer.Visible = (index == 2);

        RefreshPage();
    }

    private void RefreshPage()
    {
        int totalItems = GetCurrentTabItemsCount();
        int totalPages = Math.Max(1, Mathf.CeilToInt((float)totalItems / SlotsPerPage));
        _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

        if (PageLabel != null)
        {
            PageLabel.Text = $"{_currentPage + 1} / {totalPages}";
            PageLabel.Modulate = new Color(1, 1, 1, totalPages > 1 ? 1f : 0f);
            
        }

        if (PrevPageButton != null)
        {
            bool canPrev = _currentPage > 0;
            PrevPageButton.Disabled = !canPrev;
            PrevPageButton.Modulate = new Color(1, 1, 1, canPrev ? 1f : 0f);
        }

        if (NextPageButton != null)
        {
            bool canNext = _currentPage < totalPages - 1;
            NextPageButton.Disabled = !canNext;
            NextPageButton.Modulate = new Color(1, 1, 1, canNext ? 1f : 0f);
        }

        switch (_currentTab)
        {
            case 0: RenderCgPage(StoryGrid, _storyCgList); break;
            case 1: RenderCgPage(BgGrid, _bgCgList); break;
            case 2: RenderMusicPage(); break;
        }
    }

    private int GetCurrentTabItemsCount()
    {
        return _currentTab switch
        {
            0 => _storyCgList.Count,
            1 => _bgCgList.Count,
            2 => _musicList.Count,
            _ => 0
        };
    }

    private void OnPrevPressed()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RefreshPage();
        }
    }

    private void OnNextPressed()
    {
        int totalItems = GetCurrentTabItemsCount();
        int totalPages = Math.Max(1, Mathf.CeilToInt((float)totalItems / SlotsPerPage));
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            RefreshPage();
        }
    }

    private void RenderCgPage(GridContainer grid, List<CgGroupData> dataList)
    {
        if (grid == null || SlotItemPrefab == null) return;

        foreach (Node chlid in grid.GetChildren())
        {
            chlid.QueueFree();
        }
        
        int startIndex = _currentPage * SlotsPerPage;
        int endIndex = Math.Min(startIndex + SlotsPerPage, dataList.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var group = dataList[i];
            var slot = SlotItemPrefab.Instantiate<GallerySlotItem>();
            grid.AddChild(slot);
            slot.Setup(group);
            slot.OnClicked += (data, unlockedPaths) =>
            {
                PreviewPanel?.OpenPreview(unlockedPaths,  data.GroupName);
            };
        }
    }

    private void RenderMusicPage()
    {
        if (_musicList == null || MusicItemPrefab == null || _musicList == null) return;
        
        foreach (Node child in MusicContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        int startIndex = _currentPage * SlotsPerPage;
        int endIndex = Math.Min(startIndex + SlotsPerPage, _musicList.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var track = _musicList[i];
            var item = MusicItemPrefab.Instantiate<MusicSlotItem>();
            MusicContainer.AddChild(item);

            bool isPlaying = track.TrackId == _currentPlayingTrackedId;
            item.Setup(track, isPlaying);
            item.OnTrackSlected += (selectedTrack) =>
            {
                PlayTrack(selectedTrack);
            };
        }
    }

    private void PlayTrack(MusicTrackData track)
    {
        if (MusicPlayer == null || string.IsNullOrEmpty(track.AudioPath)) return;
        if (!FileAccess.FileExists(track.AudioPath)) return;
        
        _currentPlayingTrackedId = track.TrackId;
        MusicPlayer.Stop();
        MusicPlayer.Stream = GD.Load<AudioStream>(track.AudioPath);
        MusicPlayer.Play();
        
        RenderMusicPage();
    }
}
