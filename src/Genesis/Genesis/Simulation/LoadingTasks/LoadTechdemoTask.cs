using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Generators;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Simulation.LoadingTasks;

public class LoadTechdemoTask : ILoadingTask
{
    public void Execute(World world, ContentManager contentManager, MapLoader mapLoader, AudioService audioService)
    {
        var metaData = world.GetResource<MetaDataComponent>();
        
        world.Clear();
        
        world.SetResource(metaData);
        
        world.Create(
            new RunSessionComponent(),
            new RunTimerComponent(),
            new RunStatsComponent()
        );

        world.SetResource(new RunStatsComponent { RunType = (int)RunType.Techdemo });

        var floorLayout = FloorGenerator.GetTechDemoFloor();
        world.Create(floorLayout);
        mapLoader.Load(
            floorLayout.CurrentRoom.Definition.MapPath,
            MutantType.Mutant1,
            floorLayout.Layer
        );
    }
}