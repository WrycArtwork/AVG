using Godot;
using System;
using System.Collections.Generic;

namespace AVG.Scripts;

public class CgGroupData
{
    public string GroupId {get; set;}
    public string GroupName {get; set;}
    public string ThumbnailPath {get; set;}
    public List<string> CgsPaths { get; set; } = new();
}

public class MusicTrackData
{
    public string TrackId {get; set;}
    public string TrackTitle {get; set;}
    public string AudioPath {get; set;}
}
