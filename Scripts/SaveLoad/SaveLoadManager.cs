using Godot;
using System;
using AVG.Scripts;

namespace AVG.Scripts;

public partial class SaveLoadManager : Node
{
    public static SaveLoadManager Instance { get;  private set; } 
    public static SaveData PendingLoadData { get; set; }
    
    public event Action OnPanelClosed;

    [Export] public SaveLoadPanel SaveLoadPanelUI;

    private ISaveableContext _currentContext;

    public override void _Ready()
    {
        SaveLoadPanelUI.Visible = false;
        Instance = this;

        if (SaveLoadPanelUI != null)
        {
            SaveLoadPanelUI.OnSaveSlotSelected += HandleSave;
            SaveLoadPanelUI.OnLoadDataSelected += HandleLoad;

            SaveLoadPanelUI.CloseButton.Pressed += () =>
            {
                SaveLoadPanelUI.Visible = false;
                OnPanelClosed?.Invoke();
            };
        }
    }
    
    public void RegisterContext(ISaveableContext context)
    {
        _currentContext = context;
    }
    
    public void UnregisterContext(ISaveableContext context)
    {
        if (_currentContext == context)
        {
            _currentContext = null;
        }
    }
    
    public void OpenSaveMenu()
    {
        SaveLoadPanelUI?.OpenPanel(SaveLoadPanel.Mode.Save);
    }

    public void OpenLoadMenu()
    {
        SaveLoadPanelUI?.OpenPanel(SaveLoadPanel.Mode.Load);
    }

    private async void HandleSave(int slotIndex, Viewport viewport)
    {
        if (_currentContext == null) return;

        SaveData data = _currentContext.CreateSaveData();
        if (data == null) return;

        if (SaveLoadPanelUI != null) SaveLoadPanelUI.Visible = false;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        SaveSystem.SaveGame(slotIndex, data, viewport);

        if (SaveLoadPanelUI != null)
        {
            SaveLoadPanelUI.Visible = true;
            SaveLoadPanelUI.RefreshPage();
        }
    }

    private void HandleLoad(SaveData data)
    {
        if (_currentContext != null)
        {
            _currentContext.RestoreFromSaveData(data);
        }
        else
        {
            PendingLoadData = data;
            GetTree().ChangeSceneToFile(GamePaths.SceneDialogue);
        }
    }
}