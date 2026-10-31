using Godot;
using System;
using Godot.Collections;

public class SaveData
{
    public int SlotIndex { get; set; } //存檔欄位編號
    public string ChapterName { get; set; } //章節名稱
    public string SaveTime {get; set;} //存檔時間
    public string ThumbnailPath { get; set; } //縮圖位置
    
    public string DialogueId { get; set; } //對話ID

    public Dictionary<string, int> GameConditions { get; set; } = new(); //條件

}
