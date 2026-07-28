using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Generators;
using Microsoft.Xna.Framework.Content;
namespace Genesis.Simulation.LoadingTasks;

public class NewGameTask(FloorGenerator floorGenerator, MutantType mutant, RandomService rng) : ILoadingTask
{
    public void Execute(World world, ContentManager contentManager, MapLoader mapLoader, AudioService audioService)
    {
        if (world is null) { throw new ArgumentNullException(nameof(world)); }
        if (contentManager is null) { throw new ArgumentNullException(nameof(contentManager)); }
        if (audioService is null) { throw new ArgumentNullException(nameof(audioService)); }

        world.Clear();

        world.SetResource(rng);

        world.SetResource(new RunSessionComponent { TutorialActive = true});

        world.SetResource(new RunTimerComponent());

        world.SetResource(new RunStatsComponent { RunType = (int)RunType.Normal });

        var bossList = CalculateBossSchedule(mutant);
        world.SetResource(new BossQueueComponent(bossList));

        var floorLayout = SetupFloor(world);
        var startingRoom = floorLayout.CurrentRoom;
        world.SetResource(floorLayout);
        mapLoader.Load(startingRoom.Definition.MapPath, mutant, 1);
        startingRoom.IsVisited = true;
    }

    private List<EnemyType> CalculateBossSchedule(MutantType playerType)
    {
        List<EnemyType> mutants = [EnemyType.Mutant1, EnemyType.Mutant2, EnemyType.Mutant3];
        var playerMutant = playerType switch
        {
            MutantType.Mutant1 => EnemyType.Mutant1,
            MutantType.Mutant2 => EnemyType.Mutant2,
            MutantType.Mutant3 => EnemyType.Mutant3,
            _ => throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null)
        };
        mutants.Remove(playerMutant);

        // Flip a coin for the two mutant bosses
        if (rng.Chance(0.5f))
        {
            (mutants[0], mutants[1]) = (mutants[1], mutants[0]);
        }

        return mutants;
    }

    private FloorLayoutComponent SetupFloor(World world)
    {
        var floorLayout = floorGenerator.GenerateFloor(1);
        world.Create(floorLayout);
        return floorLayout;
    }
}