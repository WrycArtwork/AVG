using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;

namespace AVG.Scripts;

public enum DeviceType
{
    KeyboardMouse,
    Xbox,
    PlayStation
}

public partial class InputPromptManager : Node
{
    public class ActionBinding
    {
        public Dictionary<string, List<string>> Keys { get; set; } = new();
        public Dictionary<string, string> Icons { get; set; } = new();
    }
    
    public static InputPromptManager Instance { get; private set; }
    
    public DeviceType CurrentDeviceType { get; private set; } = DeviceType.KeyboardMouse;
    public event Action<DeviceType> OnDeviceTypeChanged;
    private Dictionary<string, ActionBinding> _iconMappings = new(StringComparer.OrdinalIgnoreCase);

    public override void _EnterTree()
    {
        Instance = this;
        LoadAndApplyInputMapping();
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _ExitTree()
    {
        Input.JoyConnectionChanged -= OnJoyConnectionChanged;
    }

    public void LoadAndApplyInputMapping()
    {
        if (!FileAccess.FileExists(GamePaths.InputMappingData)) return;
        
        using var file = FileAccess.Open(GamePaths.InputMappingData, FileAccess.ModeFlags.Read);
        if (file == null) return;

        var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true};
        _iconMappings = JsonSerializer.Deserialize<Dictionary<string, ActionBinding>>(file.GetAsText(), option) ?? new(StringComparer.OrdinalIgnoreCase);
        RegisterActionsToInputMap();
    }

    private void RegisterActionsToInputMap()
    {
        foreach (var (action, binding) in _iconMappings)
        {
            if (!InputMap.HasAction(action)) InputMap.AddAction(action);
            else InputMap.ActionEraseEvents(action);
            
            if (binding.Keys == null) continue;
            if (binding.Keys.TryGetValue(nameof(DeviceType.KeyboardMouse), out var kmKeys))
            {
                foreach (var key in kmKeys)
                {
                    var inputEvent = ParseKeyboardMouseEvent(key);
                    if (inputEvent != null)
                    {
                        InputMap.ActionAddEvent(action, inputEvent);
                    }
                }
            }

            List<string> joyKeys = null;
            if (binding.Keys.TryGetValue(nameof(DeviceType.Xbox), out var xKeys)) joyKeys = xKeys;
            else if (binding.Keys.TryGetValue(nameof(DeviceType.PlayStation), out var psKeys)) joyKeys = psKeys;

            if (joyKeys != null)
            {
                foreach (var key in joyKeys)
                {
                    var inputEvent = ParseJoypadEvent(key);
                    if (inputEvent != null)
                    {
                        InputMap.ActionAddEvent(action, inputEvent);
                    }
                }
            }
        }
    }

    private InputEvent ParseKeyboardMouseEvent(string key)
    {
        if (key.Equals("WheelUp", StringComparison.OrdinalIgnoreCase))
            return new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp };
            
        if (key.Equals("WheelDown", StringComparison.OrdinalIgnoreCase))
            return new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown };
            
        if (key.Equals("MouseLeft", StringComparison.OrdinalIgnoreCase))
            return new InputEventMouseButton { ButtonIndex = MouseButton.Left };
            
        if (key.Equals("MouseRight", StringComparison.OrdinalIgnoreCase))
            return new InputEventMouseButton { ButtonIndex = MouseButton.Right };

        if (Enum.TryParse<Key>(key, true, out var keycode) && keycode != Key.None)
        {
            return new InputEventKey { PhysicalKeycode = keycode };
        }
        
        return null;
        
    }

    private InputEvent ParseJoypadEvent(string key)
    {
        if (Enum.TryParse<JoyButton>(key, true, out var button))
        {
            return new InputEventJoypadButton { ButtonIndex = button };
        }
        return null;
    }
    
    public override void _Input(InputEvent @event)
    {
        DeviceType detected = CurrentDeviceType;

        if (@event is InputEventKey || @event is InputEventMouseButton)
        {
            detected = DeviceType.KeyboardMouse;
        }
        else if (@event is InputEventJoypadButton ||
                 @event is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > 0.4f)
        {
            detected = DetectJoypadBrand(@event.Device);
        }

        if (detected != CurrentDeviceType)
        {
            CurrentDeviceType = detected;
            OnDeviceTypeChanged?.Invoke(CurrentDeviceType);
        }
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        if (!connected && Input.GetConnectedJoypads().Count == 0)
        {
            CurrentDeviceType = DeviceType.KeyboardMouse;
            OnDeviceTypeChanged?.Invoke(CurrentDeviceType);
        }
    }

    private DeviceType DetectJoypadBrand(int deviceId)
    {
        string joyName = Input.GetJoyName(deviceId).ToLowerInvariant();
        if (joyName.Contains("ps4") ||
            joyName.Contains("ps5") ||
            joyName.Contains("sony") || 
            joyName.Contains("dualshock") ||
            joyName.Contains("dualsense"))
        {
            return DeviceType.PlayStation;
        }
        return DeviceType.Xbox;
    }

    public string GetActionIcon(string actionName)
    {
        if (string.IsNullOrEmpty(actionName)) return null;
        
        string deviceKey = CurrentDeviceType.ToString();
        string fileName = null;

        if (_iconMappings.TryGetValue(actionName, out var binding) && binding.Icons != null)
        {
            if (binding.Icons.TryGetValue(deviceKey, out var mappedFileName))
            {
                fileName = mappedFileName;
            }
            
            else if (binding.Icons.TryGetValue(nameof(DeviceType.KeyboardMouse), out var fallbackFileName))
            {
                fileName = fallbackFileName;
                deviceKey = nameof(DeviceType.KeyboardMouse);
            }
        }

        fileName ??= actionName;
        string iconDir = $"{GamePaths.IconDir}/{deviceKey}";
        string path = GamePaths.GetValidImage(iconDir, fileName);
        if (!FileAccess.FileExists(path))
        {
            path = GamePaths.GetValidImage($"{GamePaths.IconDir}/{nameof(DeviceType.KeyboardMouse)}", fileName);
        }
        return path;
    }
}
