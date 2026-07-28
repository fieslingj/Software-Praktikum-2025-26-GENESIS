using System;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Generators;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Simulation.LoadingTasks;

public class SwitchLayerTask() : ILoadingTask
{
    public void Execute(World world, ContentManager contentManager, MapLoader mapLoader, AudioService audioService)
    {
        if (world is null) { throw new ArgumentNullException(nameof(world)); }
        if (contentManager is null) { throw new ArgumentNullException(nameof(contentManager)); }
        if (audioService is null) { throw new ArgumentNullException(nameof(audioService)); }

        // Extract persistant data
        var playerData = SavedPlayerData.Fetch(world);
        var bossQueue = world.GetResource<BossQueueComponent>();
        var savedRunStats = world.GetResource<RunStatsComponent>();
        var savedSession = world.GetResource<RunSessionComponent>();
        var savedRunTimer = world.GetResource<RunTimerComponent>();
        var randomService = world.GetResource<RandomService>();

        // Extract tutorial progress if it exists
        var tutorialProgressQuery = new QueryDescription().WithAll<Genesis.Gameplay.Components.Tutorial.TutorialProgressComponent>();
        var savedTutorialProgress = new Genesis.Gameplay.Components.Tutorial.TutorialProgressComponent();
        var hasTutorialProgress = false;
        world.Query(in tutorialProgressQuery, (ref Genesis.Gameplay.Components.Tutorial.TutorialProgressComponent progress) =>
        {
            savedTutorialProgress = progress;
            hasTutorialProgress = true;
        });
        
        //next layer
        var layerNumber = world.GetResource<FloorLayoutComponent>().Layer + 1;
        
        // Clear the world
        world.Clear();
        
        //generate floor
        var floorLayout = SetupFloor(world,layerNumber);
        
        //set resource
        var startingRoom = floorLayout.CurrentRoom;
        startingRoom.IsVisited = true;
        world.SetResource(floorLayout);
        world.SetResource(bossQueue);
        world.SetResource(savedSession);
        world.SetResource(savedRunTimer);
        world.SetResource(savedRunStats);
        world.SetResource(randomService);

        //load room
        mapLoader.Load(
            startingRoom.Definition.MapPath,
            playerData.MutantType.Type,
            layerNumber,
            DoorDirection.Elevator,
            savedPlayerData: playerData
        );

        // Restore tutorial progress if it existed
        if (hasTutorialProgress)
        {
            world.Create(savedTutorialProgress);
        }
    }
    
    private FloorLayoutComponent SetupFloor(World world, int layerNumber)
    {
        var rng = new RandomService(Environment.TickCount);
        var floorGenerator = new FloorGenerator(rng);
        
        var floorLayout = floorGenerator.GenerateFloor(layerNumber);
        world.Create(floorLayout);
        return floorLayout;
    }
}