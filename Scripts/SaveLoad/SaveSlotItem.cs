using Godot;
using System;

public partial class SaveSlotItem : Button
{
    [Export] public TextureRect Thumbnail;
    [Export] public Label SlotNumLabel;
    [Export] public Label ChapterLabel;
    [Export] public Label TimeLabel;
    
    public int SlotIndex { get; private set; }
    public event Action<int> OnClicked;

    public override void _Ready()
    {
        Pressed += () => OnClicked?.Invoke(SlotIndex);
    }

    public void Setup(int slotIndex, SaveData data)
    {
        SlotIndex = slotIndex;
        if(SlotNumLabel != null) SlotNumLabel.Text = (slotIndex == 0)? "AUTO SAVE" : $"NO. {slotIndex:D2}";

        if (data == null)
        {
            if (ChapterLabel != null) ChapterLabel.Text = "";
            if (TimeLabel != null) TimeLabel.Text = "----/--/-- --:--";
            if (Thumbnail != null) Thumbnail.Texture = null;
        }
        else
        {
            if (ChapterLabel != null) ChapterLabel.Text = data.ChapterName;
            if (TimeLabel != null) TimeLabel.Text = data.SaveTime;
            if (Thumbnail != null && FileAccess.FileExists(data.ThumbnailPath))
            {
                var img = Image.LoadFromFile(data.ThumbnailPath);
                Thumbnail.Texture = ImageTexture.CreateFromImage(img);
            }
        }
    }
}
