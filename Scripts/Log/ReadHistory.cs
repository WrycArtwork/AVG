using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using AVG.Scripts;

public static class ReadHistory
{
    private const string FilePath = GamePaths.GalleryData;
    private static readonly HashSet<string> _readDialogueIds = new(StringComparer.OrdinalIgnoreCase);

    public static void MarkAsRead(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _readDialogueIds.Add(id.Trim());
    }

    public static bool IsRead(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _readDialogueIds.Contains(id.Trim());
    }

    public static void Clear()
    {
        _readDialogueIds.Clear();
    }
}


