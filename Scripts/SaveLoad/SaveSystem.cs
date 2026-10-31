using Godot;
using System;
using System.Text.Json;

public static class SaveSystem
{
    public static string GetSaveJsonPath(int slotIndex) => $"user://save_{slotIndex:D2}.json";
    public static string GetThumbPath(int slotIndex) => $"user://thumb_{slotIndex:D2}.jpg";

    public static void SaveGame(int slotIndex, SaveData data, Viewport viewport)
    {
        data.SlotIndex = slotIndex;
        data.SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.ThumbnailPath = GetThumbPath(slotIndex);
        
        SaveThumbnail(viewport, data.ThumbnailPath);
        
        string json = JsonSerializer.Serialize(data,  new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(GetSaveJsonPath(slotIndex), FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }

    public static SaveData LoadGame(int slotIndex)
    {
        string path = GetSaveJsonPath(slotIndex);
        if (!FileAccess.FileExists(path)) return null;
        
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return null;
        
        return JsonSerializer.Deserialize<SaveData>(file.GetAsText());
    }
    

    private static void SaveThumbnail(Viewport viewport, string thumbnailPath)
    {
        var img = viewport.GetTexture().GetImage();
        if (img == null) return;
        
        img.Resize(320, 180, Image.Interpolation.Bilinear);
        img.SaveJpg(thumbnailPath);
    }
}
