using System;
using System.Text.Json.Serialization;

namespace Genesis.Gameplay.Level;

public enum RoomType
{
    Start,
    Common,
    Boss,
    TechDemo,
}

[Serializable]
public struct RoomDefinition
{
    [JsonConstructor]
    public RoomDefinition(string id, string mapPath, RoomType type)
    {
        Id = id;
        MapPath = mapPath;
        Type = type;
    }

    public required string Id { get; set; }
    public required string MapPath { get; set; }
    public required RoomType Type { get; set; }
}