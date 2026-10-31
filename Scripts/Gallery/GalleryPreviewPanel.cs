using Godot;
using System;
using System.Collections.Generic;

namespace AVG.Scripts;

public partial class GalleryPreviewPanel : Control
{
    [ExportGroup("Display")]
    [Export] public TextureRect FullImageView;
    [Export] public Label ImageLabel;
    [Export] public Label PageLabel;
    
    [ExportGroup("Buttons")]
    [Export] public Button PrevButton;
    [Export] public Button NextButton;
    [Export] public Button CloseButton;
    [Export] public Button ToggleButton;
    
    [ExportGroup("UI Container")]
    [Export] public Container UiContainer;

    private List<string> _currentCgs = new();
    private string _currentGroupName = "";
    private int _currentPage = 0;
    private bool _isUiHidden = false;

    public override void _Ready()
    {
        Visible = false;

        if (PrevButton != null) PrevButton.Pressed += ShowPrevious;
        if (NextButton != null) NextButton.Pressed += ShowNext;
        if (CloseButton != null) CloseButton.Pressed += ClosePreview;
        if (ToggleButton != null) ToggleButton.Pressed += ToggleUiVisibility;
    }

    public void OpenPreview(List<string> unlockedCgs,  string groupName = "")
    {
        if (unlockedCgs == null || unlockedCgs.Count == 0) return;
        _currentCgs = unlockedCgs;
        _currentGroupName = groupName;
        _currentPage = 0;
        _isUiHidden = false;
        if (UiContainer != null) UiContainer.Visible = true;
        
        Visible = true;
        DisplayCurrentCg();
    }

    private void DisplayCurrentCg()
    {
        if (_currentPage < 0 || _currentPage >= _currentCgs.Count) return;
        
        string path = _currentCgs[_currentPage];
        if (FileAccess.FileExists(path))
        {
            FullImageView.Texture = GD.Load<Texture2D>(path);
        }

        if (ImageLabel != null)
        {
            ImageLabel.Text = _currentGroupName;
        }

        if (PageLabel != null)
        {
            PageLabel.Visible = _currentCgs.Count > 1;
            PageLabel.Text = $"{_currentPage + 1} / {_currentCgs.Count}";
        }
        
        if (PrevButton !=  null) PrevButton.Visible = _currentCgs.Count > 1;
        if (NextButton !=  null) NextButton.Visible = _currentCgs.Count > 1;
    }

    private void ShowNext()
    {
        if (_currentCgs.Count <= 1) return;
        _currentPage = (_currentPage + 1) % _currentCgs.Count;
        DisplayCurrentCg();
    }

    private void ShowPrevious()
    {
        if (_currentCgs.Count <= 1) return;
        _currentPage = (_currentPage - 1 + _currentCgs.Count) % _currentCgs.Count;
        DisplayCurrentCg();
    }

    public void ToggleUiVisibility()
    {
        _isUiHidden = !_isUiHidden;
        if (UiContainer != null) UiContainer.Visible = !_isUiHidden;
    }

    public void ClosePreview()
    {
        Visible = false;
        FullImageView.Texture = null;
        _currentCgs.Clear();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey { Pressed: true, Echo: false} keyEvent)
        {
            if (keyEvent.Keycode == Key.Backspace)
            {
                ToggleUiVisibility();
                GetViewport().SetInputAsHandled();
                return;
            }
            
            if (!_isUiHidden)
            {
                if (keyEvent.Keycode == Key.Left) { ShowPrevious(); GetViewport().SetInputAsHandled(); return;}
                if (keyEvent.Keycode == Key.Right) { ShowNext(); GetViewport().SetInputAsHandled(); return;}
            }

            if (keyEvent.Keycode == Key.Escape)
            {
                ClosePreview();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseButton { Pressed: true } mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (_isUiHidden)
                {
                    ToggleUiVisibility();
                    GetViewport().SetInputAsHandled();
                    return;
                }
                
                var hovered = GetViewport().GuiGetHoveredControl();
                if (hovered is Button) return;
                
                if (_currentCgs.Count > 1)
                {
                    ShowNext();
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        
    }
}
