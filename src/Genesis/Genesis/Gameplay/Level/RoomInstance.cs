using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Genesis.Gameplay.Components;
using Genesis.Persistence.Run;

namespace Genesis.Gameplay.Level;

[Serializable]
public class RoomInstance
{
    [JsonConstructor]
    public RoomInstance(RoomDefinition definition, PathDirection doors = PathDirection.None)
    {
        Definition = definition;
        Doors = doors;
        IsCleared = (definition.Type == RoomType.Start);
    }

    public RoomDefinition Definition { get; set; }
    public bool IsVisited { get; set; } = false;
    public bool IsCleared { get; set; }
    
    // bitmask
    public PathDirection Doors { get; set; }

    public List<SavedEnemyData> Enemies { get; set; } = new();
    public List<SavedTrapData> Traps { get; set; } = new();
    public List<SavedCorpseData> Corpses { get; set; } = new();
    public List<SavedExplosivesData> RemoteExplosives { get; set; } = new();
}

[Flags]
public enum PathDirection : byte
{
    None  = 0,
    North = 1 << 0,
    East  = 1 << 1,
    South = 1 << 2,
    West  = 1 << 3,
    
    All = North | East | South | West,
    Vertical = North | South,
    Horizontal = East | West,
}