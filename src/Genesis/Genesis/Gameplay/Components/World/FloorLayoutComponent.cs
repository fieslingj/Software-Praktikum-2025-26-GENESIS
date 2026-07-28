using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Genesis.Gameplay.Level;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Components.World;

[Serializable]
public class FloorLayoutComponent
{
    [JsonIgnore]
    public Dictionary<Point, RoomInstance> Rooms { get; } = new();

    [JsonPropertyName("Rooms")]
    public Dictionary<string, RoomInstance> RoomsSerializable
    {
        get => Rooms.ToDictionary(kv => $"{kv.Key.X},{kv.Key.Y}", kv => kv.Value);
        set
        {
            Rooms.Clear();
            foreach (var kv in value)
            {
                var parts = kv.Key.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out var x) && int.TryParse(parts[1], out var y))
                {
                    Rooms[new Point(x, y)] = kv.Value;
                }
            }
        }
    }

    public Point CurrentGridPosition { get; set; } = Point.Zero;
    
    [JsonIgnore]
    public RoomInstance CurrentRoom => Rooms[CurrentGridPosition];

    // Layer Number of the Layout 
    public int Layer { get; set; }

    public static bool AllVisitedCleared(FloorLayoutComponent floorLayoutComponent)
    {
        foreach(var room in floorLayoutComponent.Rooms.Values)
        {
            if (room.IsVisited && !room.IsCleared) {return false;}
        }
        return true;
    }
}