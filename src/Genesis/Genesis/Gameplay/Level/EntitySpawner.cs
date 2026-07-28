#nullable enable
using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Entities;
using Genesis.Gameplay.Extensions;
using Genesis.Persistence.Run;
using Genesis.Architecture;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using System.Diagnostics;

namespace Genesis.Gameplay.Level;

/// <summary>
/// Handles spawning of entities (Player, Enemies, Traps, Props) from Tiled Map data.
/// </summary>
public class EntitySpawner(World world, ContentManager content, AudioService sounds, GridMap gridMap)
{
    private readonly TrapFactory mTrapFactory = new(content);
    private readonly PlayerFactory mPlayerFactory = new(content);
    private readonly EnemyFactory mEnemyFactory = new(content);
    private readonly CorpseFactory mCorpseFactory = new(content, gridMap);
    private readonly CompanionFactory mCompanionFactory = new(content);
    private readonly ExplosivesFactory mExplosivesFactory = new(content, sounds);

    private readonly Dictionary<DoorDirection, Vector2> mSpawnPositions = new();
    private Vector2 mDefaultSpawnPosition;

    private FloorLayoutComponent mFloor = null!;
    private bool mRoomIsVisited;

    /// <summary>
    /// Processes the "Entities" object layer from the Tiled map.
    /// </summary>
    public void ProcessEntityLayer(TiledMap map)
    {
        mFloor = world.GetResource<FloorLayoutComponent>();

        // Capture the visited state BEFORE any processing
        // This ensures entities spawn correctly on first visit
        mRoomIsVisited = mFloor.CurrentRoom.IsVisited;

        var layerNumber = mFloor.Layer;

        //get texture of current layer

        Texture2D tilesetTexture = content.Load<Texture2D>("Maps/tileset_ebene2_png");
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

        var entityLayer = map.GetLayer<TiledMapObjectLayer>("Entities");
        if (entityLayer is null)
        {
            Console.Error.WriteLine("[EntitySpawner] No ObjectLayer 'Entities' found!");
            return;
        }

        if (mRoomIsVisited)
        {
            foreach (var enemyData in mFloor.CurrentRoom.Enemies)
            {
                mEnemyFactory.Recreate(world, enemyData);
            }

            foreach (var corpseData in mFloor.CurrentRoom.Corpses)
            {
                mCorpseFactory.Recreate(world, corpseData);
            }

            foreach (var trapData in mFloor.CurrentRoom.Traps)
            {
                mTrapFactory.Recreate(world, trapData);
            }

            foreach (var explosiveData in mFloor.CurrentRoom.RemoteExplosives)
            {
                mExplosivesFactory.CreateRemoteExplosivePlaced(world, explosiveData.Position.Value);
            }
        }

        // Create a dictionary of objects by their Identifier for easy lookup (needed for traps referencing effects)
        var objectsById = new Dictionary<int, TiledMapObject>(capacity: entityLayer.Objects.Length);
        foreach (var obj in entityLayer.Objects)
        {
            objectsById[obj.Identifier] = obj;
        }

        var gridMap = world.GetResource<GridMap>();
        if (gridMap == null)
        {
            Console.WriteLine("[EntitySpawner] Warning: GridMap resource is missing!");
        }

        foreach (var entity in entityLayer.Objects)
        {
            switch (entity.Type)
            {
                case "Trap":
                    if (!mRoomIsVisited) {SpawnTrap(entity, objectsById);}
                    break;

                case "SnackMachine":
                    var sprite = content.Load<Texture2D>("Maps/props_and_items_ebene2_png");
                    SnackMachineEntity.Create(world, entity.Position, sprite, gridMap);
                    break;

                case "Door":
                    SpawnDoor(entity, tilesetTexture);
                    break;
                case "ElevatorSpawn":
                    SpawnElevator(entity);
                    break;

                case "ChemicalTank":
                    SpawnChemicalTank(entity);
                    break;

                case "Table":
                    SpawnTableEntity(entity);
                    break;

                default:
                    SpawnActor(entity);
                    break;
            }
        }
    }

    /// <summary>
    /// Spawns the player. If saved data is provided, it recreates the player from that data.
    /// Otherwise, it creates a new player at the default or specified entry point.
    /// </summary>
    public void SpawnPlayer(MutantType mutant, DoorDirection? entrypoint, SavedPlayerData? savedData = null)
    {
        if (savedData != null)
        {
            Vector2? companionSpawn = null;
            if (entrypoint.HasValue && mSpawnPositions.TryGetValue(entrypoint.Value, out var spawnPos))
            {
                var newData = new SavedPlayerData
                {
                    MutantType = savedData.MutantType,
                    Position = new PositionComponent(spawnPos),
                    Health = savedData.Health,
                    Stamina = savedData.Stamina,
                    Mass = savedData.Mass,
                    Coins = savedData.Coins,
                    Ammo = savedData.Ammo,
                    Inventory = savedData.Inventory,
                    BloodlustTracker = savedData.BloodlustTracker,
                };
                mPlayerFactory.Recreate(world, newData);
                companionSpawn = spawnPos;
            }
            else
            {
                mPlayerFactory.Recreate(world, playerData: savedData);
                companionSpawn = savedData.Position.Value;
            }

            SpawnCompanions(savedData.Companions, companionSpawn);
        }
        else
        {
            var spawnPos = (entrypoint is null) ? mDefaultSpawnPosition : mSpawnPositions[entrypoint.Value];
            mPlayerFactory.CreateNew(world, spawnPos, mutant);
        }
    }

    private void SpawnTrap(TiledMapObject trapDef, Dictionary<int, TiledMapObject> objectsById)
    {
        var props = trapDef.Properties;

        if (!props.TryGetValue("Effect", out TiledMapPropertyValue? effectProp) || effectProp?.Value is null) { return; }
        if (!int.TryParse(effectProp.Value, out var effectId)) { return; }
        if (!objectsById.TryGetValue(effectId, out var effectEntity)) { return; }
        if (!props.TryGetValue("Damage", out var damageStr) || string.IsNullOrWhiteSpace(damageStr)) { return; }
        if (!float.TryParse(damageStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var damage)) { return; }

        mTrapFactory.Create(world, trapDef.Position, TrapType.Bomb, damage, effectEntity.Position, true);
    }


    private void SpawnElevator(TiledMapObject elevatorDef)
    {
        var texture = content.Load<Texture2D>("Sprites/Props/elevator");
        var isOpen = false;
        if (elevatorDef.Properties.TryGetValue("Open", out var open))
        {
            if (open == "true") { isOpen = true; }
        }

        ElevatorEntity.Create(world, elevatorDef, texture, isOpen);

        var doorCenter = elevatorDef.Position + new Vector2(elevatorDef.Size.Width, elevatorDef.Size.Height) / 2f;
        var triggerSize = new Vector2(elevatorDef.Size.Width, 10);

        var triggerOffset = new Vector2(0, -10f);
        world.Create(
            new PositionComponent(doorCenter + triggerOffset),
            new ColliderComponent(triggerSize, isSensor: true),
            new ElevatorTriggerComponent()
        );

        mSpawnPositions[DoorDirection.Elevator] = doorCenter + new Vector2(0, 32);
    }
    private void SpawnDoor(TiledMapObject doorDef, Texture2D tilesetTexture)
    {
        var floorEntity = world.GetFirstEntity(new QueryDescription().WithExclusive<FloorLayoutComponent>());
        var floor = world.Get<FloorLayoutComponent>(floorEntity);
        var currentRoom = floor.CurrentRoom;

        if (!doorDef.Properties.TryGetValue("DoorDirection", out TiledMapPropertyValue? doorDirectionStr)) { return; }
        if (!Parser.TryParsePath(doorDirectionStr.Value, out var pathDirection)) { return; }
        if (!Parser.TryParseDoor(doorDirectionStr.Value, out var doorDirection)) { return; }

        if (currentRoom.Doors.HasFlag(pathDirection))
        {
            string doorAssetPath = floor.Layer switch
            {
                1 => "Sprites/Props/Door1",
                2 => "Sprites/Props/Door2",
                3 => "Sprites/Props/Door3",
                _ => "Sprites/Props/Door2"
            };
            var texture = content.Load<Texture2D>(doorAssetPath);

            var neighborRoom = mFloor.Rooms[mFloor.CurrentGridPosition + doorDirection.ToPoint()];
            var isOpen = neighborRoom.IsVisited;
            DoorEntity.Create(world, doorDef, texture, isOpen);

            var doorCenter = doorDef.Position + new Vector2(doorDef.Size.Width, doorDef.Size.Height) / 2f;
            var mainTriggerSize = new Vector2(doorDef.Size.Width, 10);

            // Assumption: Only doors with doorDirection south are on a south wall
            var triggerOffset = doorDirection is DoorDirection.South or DoorDirection.West ? new Vector2(0, 30f) : new Vector2(0, -10f);
            var mainTriggerPos = doorCenter + triggerOffset;

            var triggersToSpawn = new List<(Vector2 Pos, Vector2 Size)> { (mainTriggerPos, mainTriggerSize) };

            // Wings
            var wingSize = new Vector2(mainTriggerSize.Y, mainTriggerSize.X);
            var wingOffset = doorDirection is DoorDirection.South or DoorDirection.West ? -5f : 5f;

            var leftPos = new Vector2(mainTriggerPos.X - mainTriggerSize.X / 2, mainTriggerPos.Y + wingOffset);
            triggersToSpawn.Add((leftPos, wingSize));

            var rightPos = new Vector2(mainTriggerPos.X + mainTriggerSize.X / 2, mainTriggerPos.Y + wingOffset);
            triggersToSpawn.Add((rightPos, wingSize));


            var gridMap = world.GetResource<GridMap>();
            // Create triggers
            foreach (var (pos, size) in triggersToSpawn)
            {
                var col = new ColliderComponent(size, isSensor: true);
                world.Create(
                    new PositionComponent(pos),
                    col,
                    new RoomTransitionTriggerComponent(doorDirection)
                );

                //set unwalkable for enemies
                gridMap?.MarkColliderAsUnwalkable(pos, col);
            }
        }
        else
        {

            WallEntity.Create(world, doorDef, tilesetTexture);
        }

        var offset = (doorDirection is DoorDirection.South or DoorDirection.West) ? new Vector2(0, -32) : new Vector2(0, 32);
        mSpawnPositions[doorDirection] = doorDef.Position + new Vector2(doorDef.Size.Width / 2f, 0) + offset;
    }

    private void SpawnChemicalTank(TiledMapObject chemicalTankDef)
    {
        var texture = content.Load<Texture2D>("Sprites/Props/ChemicalTank");
        ChemicalTankEntity.Create(world, chemicalTankDef, texture);
    }

    private void SpawnTableEntity(TiledMapObject tableDef)
    {
        var texture = content.Load<Texture2D>("Sprites/Props/TableStanding");
        TableEntity.Create(world, tableDef, texture);
    }

    private void SpawnRobotCorpse(TiledMapObject corpseDef, GridMap? gridMap)
    {
        if (gridMap == null)
        {
            Debug.WriteLine("[EntitySpawner] Warning: Cannot spawn RobotCorpse - GridMap is missing!");
            return;
        }

        var rng = world.GetResource<RandomService>();
        mCorpseFactory.Create(
            world,
            corpseDef.Position,
            EnemyType.Robot,
            ammo: 5,
            rng
        );
    }

    private void SpawnCompanions(List<SavedCompanionData> companions, Vector2? playerPosition)
    {
        foreach (var companion in companions)
        {
            var position = playerPosition ?? mDefaultSpawnPosition;
            var newData = new SavedCompanionData()
            {
                Position = new PositionComponent(position),
                Health = companion.Health,
                Type = companion.Type,
                Ammo = companion.Ammo,
                Inventory = companion.Inventory,
            };
            mCompanionFactory.Recreate(world, newData);
        }
    }

    private void SpawnActor(TiledMapObject spawner)
    {
        switch (spawner.Name)
        {
            case "PlayerSpawn":
                mDefaultSpawnPosition = spawner.Position;
                break;

            case "RobotCorpse":
                // Only spawn robot corpse if the room was not already visited
                if (!mRoomIsVisited)
                {
                    var gridMap = world.GetResource<GridMap>();
                    SpawnRobotCorpse(spawner, gridMap);
                }
                break;

            case "ScientistSpawn":
            case "SecuritySpawn":
            case "RobotSpawn":
            case "CEOSpawn":
                if (!mRoomIsVisited)
                {
                    var type = spawner.Name switch
                    {
                        "ScientistSpawn" => EnemyType.Scientist,
                        "SecuritySpawn" => EnemyType.Security,
                        "RobotSpawn" => EnemyType.Robot,
                        "CEOSpawn" => EnemyType.Ceo,
                        _ => EnemyType.Scientist
                    };
                    mEnemyFactory.Create(world, spawner.Position, type);
                }
                break;

            case "BossSpawn":
                {
                    if (mRoomIsVisited || mFloor.CurrentRoom.Definition.Type != RoomType.Boss) { break; }

                    var bossQueue = world.GetResource<BossQueueComponent>();

                    if (bossQueue.RemainingBosses.Count > 0)
                    {
                        mEnemyFactory.Create(
                            world,
                            spawner.Position,
                            bossQueue.RemainingBosses[0]
                        );
                        bossQueue.RemainingBosses.RemoveAt(0);
                    }
                    else
                    {
                        Console.WriteLine("[EntitySpawner] BossSpawn active, but no remaining boss in the queue");
                    }
                    break;
                }
        }
    }
}

internal static class Parser
{
    public static bool TryParsePath(string input, out PathDirection result)
    {
        return Enum.TryParse(input, true, out result);
    }

    public static bool TryParseDoor(string input, out DoorDirection result)
    {
        return Enum.TryParse(input, true, out result);
    }

    public static Point ToPoint(this DoorDirection direction)
    {
        return direction switch
        {
            DoorDirection.North => new Point(0, -1),
            DoorDirection.East => new Point(1, 0),
            DoorDirection.South => new Point(0, 1),
            DoorDirection.West => new Point(-1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}