using Godot;
using System;
using System.Collections.Generic;

public partial class SaveLoadPanel : Control
{
    public enum Mode { Save, Load }
    
    [ExportGroup("Interface")]
    [Export] public Label TitleLabel;
    [Export] public Container SlotsContainer;
    [Export] public int SlotsPerPage = 8;
    [Export] public int  TotalPages = 10;
    [Export] public PackedScene SlotItemPrefab;
    [Export] public Label PageLabel;
    
    [ExportGroup("Buttons")]
    [Export] public Button CloseButton;
    [Export] public Button PrevPageButton;
    [Export] public Button NextPageButton;

    public int CurrentPage = 1;
    public Mode CurrentMode { get; private set; }= Mode.Load;
    
    public event Action<int, Viewport> OnSaveSlotSelected;
    public event Action<SaveData> OnLoadDataSelected;
    
    private readonly List<SaveSlotItem> _saveSlotItems = new();

    public override void _Ready()
    {
        if (CloseButton != null) CloseButton.Pressed += () => Visible = false;
        if (PrevPageButton != null) PrevPageButton.Pressed += OnPrevPressed;
        if (NextPageButton != null) NextPageButton.Pressed += OnNextPressed;

        CreateSlots();
        RefreshPage();
    }
    

    public void OpenPanel(Mode mode, bool disableSaveSwitch = false)
    {
        CurrentMode = mode;
        Visible = true;
        CurrentPage = 1;

        if (TitleLabel != null) TitleLabel.Text = (mode == Mode.Save) ? "Save" : "Load";
        
        RefreshPage();
    }
    

    public void CreateSlots()
    {
        foreach (Node child in SlotsContainer.GetChildren()) child.QueueFree();
        _saveSlotItems.Clear();
        
        for(int i = 0; i < SlotsPerPage; i++)
        {
            var slotNode = SlotItemPrefab.Instantiate<SaveSlotItem>();
            slotNode.OnClicked += OnSlotClicked;
            
            SlotsContainer.AddChild(slotNode);
            _saveSlotItems.Add(slotNode);
        }
    }

    public void RefreshPage()
    {
        if (PageLabel != null)
        {
            PageLabel.Text = $"{CurrentPage}/{TotalPages}";
            PageLabel.Modulate = new Color(1, 1, 1, TotalPages > 1 ? 1.0f : 0.0f);
        }

        if (PrevPageButton != null)
        {
            bool canPrev = CurrentPage > 1;
            PrevPageButton.Disabled = !canPrev;
            PrevPageButton.Modulate = new Color(1, 1, 1, canPrev ? 1.0f : 0.0f);
        }

        if (NextPageButton != null)
        {
            bool canNext = CurrentPage < TotalPages;
            NextPageButton.Disabled = !canNext;
            NextPageButton.Modulate = new Color(1, 1, 1, canNext ? 1.0f : 0.0f);
        }

        for (int i = 0; i < SlotsPerPage; i++)
        {
            int SlotIndex = CalculateSlotIndex(CurrentPage, i);
            var slotItem = _saveSlotItems[i];
            var data = SaveSystem.LoadGame(SlotIndex);
            
            slotItem.Setup(SlotIndex, data);

            if (CurrentMode == Mode.Save)
            {
                slotItem.Disabled = (SlotIndex == 0);
            }
            else
            {
                slotItem.Disabled = (data == null);
            }
        }
    }
    
    private void OnSlotClicked(int slotIndex)
    {
        if (CurrentMode == Mode.Save)
        {
            OnSaveSlotSelected?.Invoke(slotIndex, GetViewport());
        }
        else
        {
            var data = SaveSystem.LoadGame(slotIndex);
            if (data != null)
            {
                OnLoadDataSelected?.Invoke(data);
                Visible = false;
            }
        }
    }

    private int CalculateSlotIndex(int page, int slotInPage)
    {
        if (page == 1)
        {
            return slotInPage;
        }
        
        return (page - 1) * SlotsPerPage + slotInPage;
    }

    private void OnPrevPressed()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            RefreshPage();
        }
    }

    private void OnNextPressed()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            RefreshPage();
        }
    }
}
