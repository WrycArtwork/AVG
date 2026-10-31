using Godot;
using System;

public partial class LogPanel : Control
{
    [Export] public Button CloseButton;
    [Export] public ScrollContainer ScrollContainer;
    [Export] public VBoxContainer ItemsContainer;
    [Export] public PackedScene LogItemPrefab;
    
    public event Action OnPanelClosed;

    public override void _Ready()
    {
        Visible = false;

        if (CloseButton != null)
        {
            CloseButton.Pressed += ClosePanel;
        }
    }

    public void OpenPanel()
    {
        Visible = true;
        
        Callable.From(() =>
        {
            if (ScrollContainer != null)
            {
                var vScrollBar = ScrollContainer.GetVScrollBar();
                ScrollContainer.ScrollVertical = (int)vScrollBar.MaxValue;
            }
        }).CallDeferred();
    }

    public void ClosePanel()
    {
        Visible = false;
        OnPanelClosed?.Invoke();
    }

    public void AddLogItem(string name, string content, string voicePath = null)
    {
        if (LogItemPrefab == null || ItemsContainer == null) return;
        
        var item = LogItemPrefab.Instantiate<LogItem>();
        ItemsContainer.AddChild(item);
        item.Setup(name, content, voicePath, isOption: false);
    }

    public void AddOptionsItem(string chosenText)
    {
        if (LogItemPrefab == null || ItemsContainer == null) return;
        var item = LogItemPrefab.Instantiate<LogItem>();
        ItemsContainer.AddChild(item);
        item.Setup(null, chosenText, null,isOption: true);
    }

    public void ClearLogItems()
    {
        if (ItemsContainer == null) return;
        foreach (Node child in ItemsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;

        if (@event.IsActionPressed("ui_cancel") ||
            (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Right))
        {
            ClosePanel();
            GetViewport().SetInputAsHandled();
        }
    }
}
