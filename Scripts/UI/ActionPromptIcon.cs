using Godot;
using System;
using AVG.Scripts;

public partial class ActionPromptIcon : TextureRect
{
    [Export] public string ActionName = "ui_accept";

    public override void _Ready()
    {
        if (InputPromptManager.Instance != null)
        {
            InputPromptManager.Instance.OnDeviceTypeChanged += UpdateIcon;
            UpdateIcon(InputPromptManager.Instance.CurrentDeviceType);   
        }
    }

    public override void _ExitTree()
    {
        if (InputPromptManager.Instance != null)
        {
            InputPromptManager.Instance.OnDeviceTypeChanged -= UpdateIcon;
        }
    }

    private void UpdateIcon(DeviceType deviceType)
    {
        if (InputPromptManager.Instance == null)
        {
            return;
        }
        string iconPath = InputPromptManager.Instance.GetActionIcon(ActionName);
        if (!string.IsNullOrEmpty(iconPath))
        {
            Texture = GD.Load<Texture2D>(iconPath);
            Visible = true;
        }
        else
        {
            Visible = false;
        }
    }
}
