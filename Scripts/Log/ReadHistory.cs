using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class ReadHistory
{
    private const string FilePath = "user://read_history.json";
    private static readonly HashSet<string> _readDialogueIds = new();

    static ReadHistory()
    {
        LoadReadHistory();
    }

    public static void MarkAsRead(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (_readDialogueIds.Add(id)) SaveReadHistory();
    }

    public static bool IsRead(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return _readDialogueIds.Contains(id);
    }

    private static void SaveReadHistory()
    {
        using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
        if (file == null) return;
        var json = JsonSerializer.Serialize(_readDialogueIds);
        file.StoreString(json);
    }

    private static void LoadReadHistory()
    {
        if (!FileAccess.FileExists(FilePath)) return;

        using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
        if (file == null) return;
        var json = file.GetAsText();
        var list = JsonSerializer.Deserialize<List<string>>(json);
        if (list != null)
        {
            _readDialogueIds.Clear();
            foreach (var id in list) _readDialogueIds.Add(id);
        }
    }
}


