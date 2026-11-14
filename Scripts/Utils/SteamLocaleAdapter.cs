using Godot;
using System;
using System.Collections.Generic;

namespace AVG.Scripts;

public static class SteamLocaleAdapter
{
    private static readonly Dictionary<string, string> SteamLanguageMap = new()
    {
        { "tchinese", "zh_TW" },
        { "schinese", "zh_CN" },
        { "english", "en" },
        { "japanese", "ja" }
    };

    public static string GetSteamLanguage(string defaultLocale = "en")
    {
        if (Engine.HasSingleton("Steam"))
        {
            var steam = Engine.GetSingleton("Steam");
            string steamLanguage = steam.Call("getCurrentGameLanguage").AsString();
            if (!string.IsNullOrEmpty(steamLanguage) &&
                SteamLanguageMap.TryGetValue(steamLanguage.ToLower(), out var mappedLocale))
            {
                return mappedLocale;
            }
        }
        
        return defaultLocale;
    }
}
