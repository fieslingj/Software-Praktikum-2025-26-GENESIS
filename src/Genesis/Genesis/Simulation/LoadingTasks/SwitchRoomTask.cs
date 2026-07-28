using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Simulation.LoadingTasks;

public class SwitchRoomTask(Point targetGridPos, DoorDirection exitPoint) : ILoadingTask
{
    public void Execute(World world, ContentManager contentManager, MapLoader mapLoader, AudioService audioService)
    {
        // Extract persistant data
        var playerData = SavedPlayerData.Fetch(world);
        var bossQueue = world.GetResource<BossQueueComponent>();
        var savedRunStats = world.GetResource<RunStatsComponent>();
        var savedSession = world.GetResource<RunSessionComponent>();
        var savedRunTimer = world.GetResource<RunTimerComponent>();
        var metaData = world.GetResource<MetaDataComponent>();  
        var randomService = world.GetResource<RandomService>();
        var floorLayout = world.GetResource<FloorLayoutComponent>();
        floorLayout.CurrentRoom.Enemies = world.FetchAllEnemies();
        floorLayout.CurrentRoom.Traps = world.FetchAllTraps();
        floorLayout.CurrentRoom.Corpses = world.FetchAllCorpses();
        floorLayout.CurrentRoom.RemoteExplosives = world.FetchAllExplosives();
        var wasVisitedBefore = floorLayout.Rooms[targetGridPos].IsVisited;
        
        // Tutorial-Progress extrahieren falls vorhanden
        var tutorialProgressQuery = new QueryDescription().WithAll<Genesis.Gameplay.Components.Tutorial.TutorialProgressComponent>();
        var savedTutorialProgress = new Genesis.Gameplay.Components.Tutorial.TutorialProgressComponent();
        var hasTutorialProgress = false;
        world.Query(in tutorialProgressQuery, (ref Genesis.Gameplay.Components.Tutorial.TutorialProgressComponent progress) =>
        {
            savedTutorialProgress = progress;
            hasTutorialProgress = true;
        });

        // If not visited before, increment rooms explored
        if (!wasVisitedBefore)
        {
            savedRunStats.RoomsExplored++;
        }

        // Clear the world
        world.Clear();

        // Create Layout
        floorLayout.CurrentGridPosition = targetGridPos;
        world.SetResource(floorLayout);

        // Floor layout updates
        world.SetResource(bossQueue);
        world.SetResource(savedSession);
        world.SetResource(savedRunTimer);
        world.SetResource(savedRunStats);
        world.SetResource(metaData);
        world.SetResource(randomService);

        // Load new room
        var inv = InvertedDirection(exitPoint);
        var mapPath = floorLayout.Rooms[targetGridPos].Definition.MapPath;
        mapLoader.Load(mapPath, playerData.MutantType.Type, floorLayout.Layer, inv, playerData);
        
        floorLayout.CurrentRoom.IsVisited = true;
        
        // Tutorial-Progress wiederherstellen
        if (hasTutorialProgress)
        {
            world.Create(savedTutorialProgress);
        }
    }

    private static DoorDirection InvertedDirection(DoorDirection door)
    {
        var inv = door switch
        {
            DoorDirection.North => DoorDirection.South,
            DoorDirection.East => DoorDirection.West,
            DoorDirection.South => DoorDirection.North,
            DoorDirection.West => DoorDirection.East,
            _ => throw new ArgumentOutOfRangeException(nameof(door), door, null)
        };
        return inv;
    }
}