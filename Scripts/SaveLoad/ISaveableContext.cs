using Godot;
using System;

public interface ISaveableContext
{
    SaveData CreateSaveData();
    
    void RestoreFromSaveData(SaveData data);
}
