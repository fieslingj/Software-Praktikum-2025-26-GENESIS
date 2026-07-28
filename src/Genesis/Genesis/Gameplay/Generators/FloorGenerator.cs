using System;
using System.Collections.Generic;
using System.Linq;
using Genesis.Architecture;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Level;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Generators;

public class FloorGenerator(RandomService rng)
{
    private const int TargetRoomCount = 4;
    private const int MaxIterations = 1000;
    private const float ResetChance = 0.3f;

    /// <summary>
    /// Generate a floor based on the Drunkards Algorithm
    /// </summary>
    public FloorLayoutComponent GenerateFloor()
    {
        var floor = new FloorLayoutComponent();
        var visited = new HashSet<Point>();
 
        // ===== STARTING ROOM =====
        var currentPosition = Point.Zero;
        CreateRoom(floor, currentPosition, RoomCatalog.PickRandomByType(rng, RoomType.Start));
        visited.Add(currentPosition);

        // ===== COMMON ROOMS =====
        var loops = 0;
        while (floor.Rooms.Count < TargetRoomCount && loops < MaxIterations)
        {
            loops++;

            var dir = GetRandomDirection();
            var newPos = currentPosition + dir;
            currentPosition = newPos;

            // Try again when the Position was already visited
            if (visited.Contains(newPos)) {continue;}
            
            CreateRoom(floor, newPos, RoomCatalog.PickRandomByType(rng, RoomType.Common));
            visited.Add(newPos);
            
            // Reset walker to center occasionally to create branching
            currentPosition = rng.Chance(ResetChance) ? Point.Zero : newPos;
        }
        
        // ===== BOSS ROOM =====
        // find the furthest room from starting room
        var bossPosition = floor.Rooms.Keys
            .OrderByDescending(p => Math.Abs(p.X) + Math.Abs(p.Y))
            .First();
        
        floor.Rooms[bossPosition] = new RoomInstance(RoomCatalog.PickRandomByType(rng, RoomType.Boss));
        
        // Update Connections & Return
        UpdateConnections(floor);
        
        // TODO DELETE
        System.Diagnostics.Debug.WriteLine("\n=== FLOOR GENERATION COMPLETE ===\n");
        foreach (var kv in floor.Rooms)
        {
            System.Diagnostics.Debug.WriteLine($"Position: {kv.Key}");
            System.Diagnostics.Debug.WriteLine($"RoomType: {kv.Value.Definition.Type}");
            System.Diagnostics.Debug.WriteLine($"Doors:    {kv.Value.Doors}\n");
        }

        return floor;
    }
    
    /// <summary>
    /// Generate a floor based on the Drunkards Algorithm only from one layer
    /// </summary>
    public FloorLayoutComponent GenerateFloor(int layerNumber)
    {
        var floor = new FloorLayoutComponent();
        var visited = new HashSet<Point>();
        
        floor.Layer  = layerNumber;
 
        // ===== STARTING ROOM =====
        var currentPosition = Point.Zero;
        CreateRoom(floor, currentPosition, RoomCatalog.PickRandomByTypeAndLayer(rng, RoomType.Start,layerNumber));
        visited.Add(currentPosition);

        // ===== COMMON ROOMS =====
        var loops = 0;
        while (floor.Rooms.Count < TargetRoomCount && loops < MaxIterations)
        {
            loops++;

            var dir = GetRandomDirection();
            var newPos = currentPosition + dir;
            currentPosition = newPos;

            // Try again when the Position was already visited
            if (visited.Contains(newPos)) {continue;}
            
            CreateRoom(floor, newPos, RoomCatalog.PickRandomByTypeAndLayer(rng, RoomType.Common,layerNumber));
            visited.Add(newPos);
            
            // Reset walker to center occasionally to create branching
            currentPosition = rng.Chance(ResetChance) ? Point.Zero : newPos;
        }
        
        // ===== BOSS ROOM =====
        // find the furthest room from starting room
        var bossPosition = floor.Rooms.Keys
            .OrderByDescending(p => Math.Abs(p.X) + Math.Abs(p.Y))
            .First();
        
        floor.Rooms[bossPosition] = new RoomInstance(RoomCatalog.PickRandomByTypeAndLayer(rng, RoomType.Boss,layerNumber));
        
        // Update Connections & Return
        UpdateConnections(floor);
        
        // TODO DELETE
        System.Diagnostics.Debug.WriteLine("\n=== FLOOR GENERATION COMPLETE ===\n");
        foreach (var kv in floor.Rooms)
        {
            System.Diagnostics.Debug.WriteLine($"Position: {kv.Key}");
            System.Diagnostics.Debug.WriteLine($"RoomType: {kv.Value.Definition.Type}");
            System.Diagnostics.Debug.WriteLine($"Doors:    {kv.Value.Doors}\n");
        }

        return floor;
    }

    public static FloorLayoutComponent GetTechDemoFloor()
    {
        var floor = new FloorLayoutComponent();
        var techDemoRoomDef = RoomCatalog.GetTechDemoRoom();
        floor.Rooms[Point.Zero] = new RoomInstance(techDemoRoomDef);
        return floor;
    }

    private void CreateRoom(FloorLayoutComponent floor, Point pos, RoomDefinition def)
    {
        var room = new RoomInstance(def);
        floor.Rooms[pos] = room;
    }

    private void UpdateConnections(FloorLayoutComponent floor)
    {
        foreach (var kv in floor.Rooms)
        {
            var room = kv.Value;
            var pos = kv.Key;
            
            room.Doors = PathDirection.None;
            if (floor.Rooms.ContainsKey(pos + new Point(0, -1))) {room.Doors |= PathDirection.North;}

            if (floor.Rooms.ContainsKey(pos + new Point(1, 0)))
            {
                room.Doors |= PathDirection.East;
            }

            if (floor.Rooms.ContainsKey(pos + new Point(0, 1)))
            {
                room.Doors |= PathDirection.South;
            }

            if (floor.Rooms.ContainsKey(pos + new Point(-1, 0)))
            {
                room.Doors |= PathDirection.West;
            }
        }
    }

    private Point GetRandomDirection()
    {
        var val = rng.Next(4);
        return val switch
        {
            0 => new Point(0, -1),
            1 => new Point(1, 0),
            2 => new Point(0, 1),
            _ => new Point(-1, 0)
        };
    }
}