using Godot;
using System;
using System.Text.Json;

namespace AVG.Scripts;

public static class SaveSystem
{
    public static void EnsureDirectoriesExist()
    {
        if (!DirAccess.DirExistsAbsolute(GamePaths.ThumbnailDir))
        {
            DirAccess.MakeDirRecursiveAbsolute(GamePaths.ThumbnailDir);
        }
    }
    
    public static void SaveGame(int slotIndex, SaveData data, Viewport viewport)
    {
        EnsureDirectoriesExist();
        data.SlotIndex = slotIndex;
        data.SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.ThumbnailPath = GamePaths.Thumbnail(slotIndex);
        
        SaveThumbnail(viewport, data.ThumbnailPath);
        
        string json = JsonSerializer.Serialize(data,  new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(GamePaths.SaveJson(slotIndex), FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }

    public static SaveData LoadGame(int slotIndex)
    {
        string path = GamePaths.SaveJson(slotIndex);
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
