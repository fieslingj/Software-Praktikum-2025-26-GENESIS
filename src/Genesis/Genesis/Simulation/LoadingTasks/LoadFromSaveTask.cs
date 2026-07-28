using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Tutorial;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Simulation.LoadingTasks;

public class LoadFromSaveTask(int slotIndex) : ILoadingTask
{
    public void Execute(World world, ContentManager contentManager, MapLoader mapLoader, AudioService audioService)
    {
        world.Clear();
        
        // Load run data
        var saveData = SaveManager.LoadRun(slotIndex);
        
        // Recreate resources
        world.SetResource(new RandomService(saveData.Seed));
        world.SetResource(saveData.Session);
        world.SetResource(saveData.RunStats);
        world.SetResource(saveData.Timer);
        world.SetResource(saveData.BossQueue);
        world.SetResource(saveData.Floor);
        
        // Load and inject metadata
        var metaData = SaveManager.LoadMeta();
        world.SetResource(new MetaDataComponent(metaData));
        
        // Restore TutorialProgressComponent from MetaData if tutorial is active
        if (saveData.Session.TutorialActive)
        {
            var tutorialSettings = metaData.TutorialSettings;
            world.Create(new TutorialProgressComponent
            {
                MovementShown = tutorialSettings.MovementShown,
                AttackingShown = tutorialSettings.AttackingShown,
                DoorInteractionShown = tutorialSettings.DoorInteractionShown,
                SnackMachineShown = tutorialSettings.SnackMachineShown,
                BearTrapInteractionShown = tutorialSettings.BearTrapInteractionShown,
                ChemicalTankInteractionShown = tutorialSettings.ChemicalTankInteractionShown,
                TableInteractionShown = tutorialSettings.TableInteractionShown,
                CorpseInteractionShown = tutorialSettings.CorpseInteractionShown,
                MutantRoomShown = tutorialSettings.MutantRoomShown,
                LastRoomPosition = tutorialSettings.LastRoomPosition
            });
        }

        var mapPath = saveData.Floor.CurrentRoom.Definition.MapPath;
        var mutant = saveData.PlayerData.MutantType.Type;
        mapLoader.Load(mapPath, mutant, saveData.Floor.Layer, savedPlayerData: saveData.PlayerData);
    }
}