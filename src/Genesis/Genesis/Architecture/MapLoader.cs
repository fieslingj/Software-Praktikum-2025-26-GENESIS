#nullable enable
using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Entities;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Level;
using Genesis.Gameplay.Navigation;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;

namespace Genesis.Architecture;

public class MapLoader(World world, ContentManager content, AudioService audio, GraphicsDevice graphics)
{
    private EntitySpawner? mEntitySpawner;

    public void Load(string mapPath, MutantType mutant,int layerNumber,
        DoorDirection? spawnPosition = null, SavedPlayerData? savedPlayerData = null)
    {
        // load map
        var map = content.Load<TiledMap>(mapPath);
        
        // delete previous map component
        var mapComponentEntity = world.GetFirstEntity(new QueryDescription().WithAll<TiledMapComponent>());
        if (mapComponentEntity != Entity.Null) { world.Destroy(mapComponentEntity); }

        // create new map component
        world.Create(new TiledMapComponent(map, graphics));
        
        // Initialize navigation
        var gridMap = new GridMap(map, precisionDivider: 2);
        var flowField = new FlowField(gridMap);
        world.SetResource(gridMap);
        world.SetResource(flowField);
        
        // Initialize entity spawner
        mEntitySpawner = new EntitySpawner(world, content, audio, gridMap);
        
        //get texture of current layer 
        var tilesetTexture = content.Load<Texture2D>("Maps/tileset_ebene2_png");
        switch (layerNumber)
        {
            case 1:
                tilesetTexture = content.Load<Texture2D>("Maps/tileset_ebene1_png");
                break;
            case 2:
                tilesetTexture = content.Load<Texture2D>("Maps/tileset_ebene2_png");
                break;
            case 3:
                tilesetTexture = content.Load<Texture2D>("Maps/tileset_ebene3_png");
                break;
        }
        
        // Process map tiles
        ProcessFloorTileLayer(map,tilesetTexture);
        ProcessFloorDecorationTileLayer(map);
        ProcessWallLayer(map,tilesetTexture);
        ProcessPropsTileLayer(map);
        
        // Process the map objects
        ProcessCollisionLayer(map);
        ProcessPropsObjectLayer(map);
        ProcessAcidsLayer(map);
        
        // Process Entities (Traps, Doors, Enemies, Player Spawn Points)
        mEntitySpawner.ProcessEntityLayer(map);
        
        // Spawn Player
        mEntitySpawner.SpawnPlayer(mutant, spawnPosition, savedPlayerData);
    }
    
    private void ProcessCollisionLayer(TiledMap map)
    {
        var collisionLayer = map.GetLayer<TiledMapObjectLayer>("Collisions");
        if (collisionLayer is null) { return; }

        // Get all colliders
        var wallRects = new List<RectangleF>();
        foreach (var obj in collisionLayer.Objects)
        {
            wallRects.Add(new RectangleF(obj.Position.X, obj.Position.Y, obj.Size.Width, obj.Size.Height));
        }

        // Collect the door rectangles
        var doorRects = GetDoorRects(map);

        // Cut each wall that contains a door
        foreach (var door in doorRects)
        {
            var newWalls = new List<RectangleF>();

            foreach (var wall in wallRects)
            {
                if (wall.Intersects(door))
                {
                    // Horizontal wall
                    if (wall.Width > wall.Height)
                    {
                        // Left piece
                        if (door.Left > wall.Left)
                        {
                            newWalls.Add(new RectangleF(wall.X, wall.Y, door.Left - wall.Left, wall.Height));
                        }
                        // Right piece
                        if (door.Right < wall.Right)
                        {
                            newWalls.Add(new RectangleF(door.Right, wall.Y, wall.Right - door.Right, wall.Height));
                        }
                    }
                    // Vertical wall
                    else 
                    {
                        // Upper piece
                        if (door.Top > wall.Top)
                        {
                            newWalls.Add(new RectangleF(wall.X, wall.Y, wall.Width, door.Top - wall.Top));
                        }
                        // Lower piece
                        if (door.Bottom < wall.Bottom)
                        {
                            newWalls.Add(new RectangleF(wall.X, door.Bottom, wall.Width, wall.Bottom - door.Bottom));
                        }
                    }
                }
                else
                {
                    newWalls.Add(wall);
                }
            }
            // Die Liste der Wände aktualisieren für den nächsten Durchlauf (falls es mehrere Türen gibt)
            wallRects = newWalls;
        }

        // Create Colliders
        foreach (var rect in wallRects)
        {
            CollisionEntity.Create(world, rect);
        }
    }
    
    private List<RectangleF> GetDoorRects(TiledMap map)
    {
        var rects = new List<RectangleF>();
        var entityLayer = map.GetLayer<TiledMapObjectLayer>("Entities");
        if (entityLayer == null) { return rects; }

        // Get the Floor layout
        var floorEntity = world.GetFirstEntity(new QueryDescription().WithExclusive<FloorLayoutComponent>());
        if (floorEntity == Entity.Null) 
        {
            Console.WriteLine("[MapLoader] Warning: No FloorLayoutComponent found. Assuming all doors are closed.");
            return rects; 
        }
        var floor = world.Get<FloorLayoutComponent>(floorEntity);
        var currentRoom = floor.CurrentRoom;

        foreach (var obj in entityLayer.Objects)
        {
            if (obj.Type != "Door") { continue; }
            if (!obj.Properties.TryGetValue("DoorDirection", out var dirPropValue)) { continue; }
            if (string.IsNullOrEmpty(dirPropValue)) { continue; }
            if (!Enum.TryParse<PathDirection>(dirPropValue, true, out var direction)) { continue; }

            // If there is a door in this direction, add the rectangle
            if (currentRoom.Doors.HasFlag(direction))
            {
                rects.Add(new RectangleF(obj.Position.X, obj.Position.Y, obj.Size.Width, obj.Size.Height));
            }
        }
        return rects;
    }

    private void ProcessAcidsLayer(TiledMap map)
    {
        var acidsLayer = map.GetLayer<TiledMapObjectLayer>("Acids");
        if (acidsLayer is null)
        {
            Console.Error.WriteLine("[MapLoader] No ObjectLayer 'Acids' found!");
            return;
        }
        
        foreach (var mapObject in acidsLayer.Objects)
        {
            AcidHazardEntity.Create(world, mapObject);
        }
    }

    private void ProcessPropsObjectLayer(TiledMap map)
    {
        var propsLayer = map.GetLayer<TiledMapObjectLayer>("Props");
        if (propsLayer is null)
        {
            Console.Error.WriteLine("[MapLoader] No ObjectLayer 'props' found!");
            return;
        }

        foreach (var tile in propsLayer.Objects)
        {
            var position = tile.Position + new Vector2(tile.Size.Width, tile.Size.Height) / 2f;
            var collider = new ColliderComponent(tile.Size);
            var gridMap = world.GetResource<GridMap>();
            gridMap?.MarkColliderAsUnwalkable(position, collider);

            world.Create(
                new PositionComponent(position),
                collider
            );
        }
    }

    private void ProcessFloorTileLayer(TiledMap map, Texture2D tilesetTexture)
    {
        const float layerDepth = 0.0000f;
        
        var floorLayer = map.GetLayer<TiledMapTileLayer>("floor");
        if (floorLayer is null)
        {
            Console.Error.WriteLine("[MapLoader] No TileLayer 'Floor' found!");
            return;
        }
        ProcessTileLayer(floorLayer, map, tilesetTexture, layerDepth);
    }
    
    private void ProcessFloorDecorationTileLayer(TiledMap map)
    {
        const float layerDepth = 0.0001f;
        
        var floorLayer = map.GetLayer<TiledMapTileLayer>("floorDecoration");
        if (floorLayer is null)
        {
            Console.Error.WriteLine("[MapLoader] No TileLayer 'FloorDecoration' found!");
            return;
        }
        var texture = content.Load<Texture2D>("Maps/props_and_items_ebene2_png");
        ProcessTileLayer(floorLayer, map, texture, layerDepth);
    }

    private void ProcessPropsTileLayer(TiledMap map)
    {
        const float layerDepth = 0.1f;
        
        var propsLayer = map.GetLayer<TiledMapTileLayer>("props");
        if (propsLayer is null)
        {
            Console.Error.WriteLine("[MapLoader] No TileLayer 'props' found!");
            return;
        }
        
        var texture = content.Load<Texture2D>("Maps/props_and_items_ebene2_png");
        ProcessTileLayer(propsLayer, map, texture, layerDepth);
    }

    private void ProcessWallLayer(TiledMap map, Texture2D tilesetTexture)
    {
        const float layerDepth = 0.09999999f;
        
        var wallLayer = map.GetLayer<TiledMapTileLayer>("walls");
        if (wallLayer is null)
        {
            Console.Error.WriteLine("[MapLoader] No TileLayer 'walls' found!");
            return;
        }
        
        
        ProcessTileLayer(wallLayer, map, tilesetTexture, layerDepth);
    }

    private void ProcessTileLayer(TiledMapTileLayer layer, TiledMap map, Texture2D layerTextures, float layerDepth)
    {
        foreach (var tile in layer.Tiles)
        {
            var id = tile.GlobalIdentifier;
            if (id == 0) { continue; }

            var tileset = map.GetTilesetByTileGlobalIdentifier(id);
            var localId = id - map.GetTilesetFirstGlobalIdentifier(tileset);
            TiledObjectFactory.Create(world, tile, tileset, layerTextures, localId, layerDepth);
        }
    }
}