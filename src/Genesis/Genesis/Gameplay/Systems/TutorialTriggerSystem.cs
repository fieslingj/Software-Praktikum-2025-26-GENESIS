using System.Threading.Tasks.Sources;
using Arch.Core;
using Arch.Core.Extensions;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Tutorial;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Overlays;
using Genesis.GameStates.Overlays.Tutorial;
using Genesis.Persistence.Meta;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Monitores and triggers tutorial events based on player actions and proximity to interactable objects.
/// </summary>
public class TutorialTriggerSystem : IUpdateSystem
{
    private readonly GameStateManager mGameStateManager;

    // Extended radius to trigger tutorials before reaching the interactable object
    private const float TutorialRadiusExtension = 10f;
    
    // Detection radius for enemy proximity
    private const float EnemyDetectionRadius = 100f;
    
    private static readonly QueryDescription sPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent, PositionComponent>();
    
    private static readonly QueryDescription sInteractableQuery = new QueryDescription()
        .WithAll<InteractableComponent, PositionComponent>();
    
    private static readonly QueryDescription sRunSessionQuery = new QueryDescription()
        .WithAll<RunSessionComponent>();
    
    private static readonly QueryDescription sTutorialProgressQuery = new QueryDescription()
        .WithAll<TutorialProgressComponent>();
    
    private static readonly QueryDescription sEnemyQuery = new QueryDescription()
        .WithAll<EnemyComponent, PositionComponent, HealthComponent>();
    
    private static readonly QueryDescription sMutantEnemyQuery = new QueryDescription()
        .WithAll<EnemyComponent, PositionComponent, HealthComponent>();

    public TutorialTriggerSystem(GameStateManager gameStateManager)
    {
        mGameStateManager = gameStateManager;
    }

    public void Update(World world, GameTime gameTime)
    {
        // Checks if the tutorial is active for the current run session
        if (!IsTutorialActive(world)) { return; }

        // If no TutorialProgressComponent exists, create one
        Entity progressEntity = Entity.Null;
        world.Query(in sTutorialProgressQuery, (Entity entity) => { progressEntity = entity; });
        if (progressEntity == Entity.Null)
        {
            progressEntity = world.Create(new TutorialProgressComponent());
        }
        
        ref var progress = ref world.Get<TutorialProgressComponent>(progressEntity);

        // Get player entity and position
        Entity player = Entity.Null;
        world.Query(in sPlayerQuery, (Entity entity) => { player = entity; });
        if (player == Entity.Null) { return; }
        
        var playerPos = world.Get<PositionComponent>(player).Value;

        // Triggers MOVEMENT tutorial if not shown yet (Entering the game)
        if (!progress.MovementShown)
        {
            TriggerTutorial(TutorialContent.MovementText);
            progress.MovementShown = true;
            SaveTutorialProgress(world, progress);
            return;
        }

        // Triggers ATTACKING tutorial if not shown yet (when near an enemy for the first time)
        if (!progress.AttackingShown && IsNearEnemy(world, playerPos))
        {
            TriggerTutorial(TutorialContent.AttackingText);
            progress.AttackingShown = true;
            SaveTutorialProgress(world, progress);
            return;
        }

        // Triggers MUTANT ROOM tutorial if not shown yet (when entering a room with a mutant enemy)
        if (!progress.MutantRoomShown && IsInMutantRoom(world))
        {
            TriggerTutorial(TutorialContent.EnteringMutantRoom);
            progress.MutantRoomShown = true;
            SaveTutorialProgress(world, progress);
            return;
        }

        // triggers proximity-based tutorial events
        CheckProximityTriggers(world, playerPos, ref progress);
    }

    private bool IsTutorialActive(World world)
    {
        Entity runSession = Entity.Null;
        world.Query(in sRunSessionQuery, (Entity entity) => { runSession = entity; });
        if (runSession == Entity.Null) { return false; }
        
        var sessionComponent = world.Get<RunSessionComponent>(runSession);
        return sessionComponent.TutorialActive;
    }

    /// <summary>
    /// Checks if the player is near any enemy entities.
    /// </summary>
    private bool IsNearEnemy(World world, Vector2 playerPos)
    {
        var detectionRadiusSq = EnemyDetectionRadius * EnemyDetectionRadius;
        var foundEnemy = false;

        world.Query(in sEnemyQuery, 
            (Entity entity, ref EnemyComponent enemy, ref PositionComponent pos, ref HealthComponent health) =>
        {
            // Ignoriere tote Feinde
            if (health.Current <= 0) { return; }

            var distanceSq = Vector2.DistanceSquared(pos.Value, playerPos);

            if (distanceSq < detectionRadiusSq)
            {
                foundEnemy = true;
            }
        });

        return foundEnemy;
    }

    /// <summary>
    /// Checks if there is a living mutant enemy in the current room.
    /// </summary>
    private bool IsInMutantRoom(World world)
    {
        var foundMutant = false;

        world.Query(in sMutantEnemyQuery, 
            (Entity entity, ref EnemyComponent enemy, ref HealthComponent health) =>
        {
            // Prüfe ob der Gegner ein Mutant ist und noch lebt
            if (health.Current <= 0) { return; }

            if (enemy.Type == Definitions.EnemyType.Mutant1 || 
                enemy.Type == Definitions.EnemyType.Mutant2 || 
                enemy.Type == Definitions.EnemyType.Mutant3)
            {
                foundMutant = true;
            }
        });

        return foundMutant;
    }

    /// <summary>
    /// Checks if the player is near any interactable objects and triggers corresponding tutorials.
    /// </summary>
    private void CheckProximityTriggers(World world, Vector2 playerPos, ref TutorialProgressComponent progress)
    {
        // Find the closest interactable entity within range
        var closestEntity = Entity.Null;
        var minDistanceSq = float.MaxValue;
        
        world.Query(in sInteractableQuery, 
            (Entity entity, ref InteractableComponent interactable, ref PositionComponent pos) =>
        {
            var distanceSq = Vector2.DistanceSquared(pos.Value, playerPos);

            // Extend the interactable radius for tutorial triggering
            var extendedRadius = interactable.Radius + TutorialRadiusExtension;
            var triggerRadiusSq = extendedRadius * extendedRadius;

            // Check if within trigger radius and closer than previous closest
            if (distanceSq < triggerRadiusSq && distanceSq < minDistanceSq)
            {
                minDistanceSq = distanceSq;
                closestEntity = entity;
            }
        });

        // No interactable object in range
        if (closestEntity == Entity.Null) { return; }

        // Trigger tutorial based on the type of interactable
        var interactable = world.Get<InteractableComponent>(closestEntity);
        
        switch (interactable.Type)
        {
            case InteractionType.Door when !progress.DoorInteractionShown:
                TriggerTutorial(TutorialContent.InteractingWithDoor);
                progress.DoorInteractionShown = true;
                SaveTutorialProgress(world, progress);
                break;
            
            case InteractionType.SnackMachine when !progress.SnackMachineShown:
                TriggerTutorial(TutorialContent.InteractingWithSnackMachine);
                progress.SnackMachineShown = true;
                SaveTutorialProgress(world, progress);
                break;
            
            case InteractionType.Corpse when !progress.CorpseInteractionShown:
                TriggerTutorial(TutorialContent.InteractingWithCorpse);
                progress.CorpseInteractionShown = true;
                SaveTutorialProgress(world, progress);
                break;
            
            case InteractionType.Trap when !progress.BearTrapInteractionShown:
                TriggerTutorial(TutorialContent.InteractingWithBearTrap);
                progress.BearTrapInteractionShown = true;
                SaveTutorialProgress(world, progress);
                break;
            
            case InteractionType.ChemicalTank when !progress.ChemicalTankInteractionShown:
                TriggerTutorial(TutorialContent.InteractingWithChemicalTank);
                progress.ChemicalTankInteractionShown = true;
                SaveTutorialProgress(world, progress);
                break;
            
            case InteractionType.Table when !progress.TableInteractionShown:
                TriggerTutorial(TutorialContent.InteractingWithTable);
                progress.TableInteractionShown = true;
                SaveTutorialProgress(world, progress);
                break;
        }
    }

    /// <summary>
    /// Triggers the tutorial overlay with the specified text.
    /// </summary>
    private void TriggerTutorial(string tutorialText)
    {
        mGameStateManager.PushState(new TutorialState(tutorialText));
    }

    private void SaveTutorialProgress(World world, TutorialProgressComponent progress)
    {
        var metaData = SaveManager.LoadMeta() ?? MetaData.NewDefault();
        world.SetResource(new MetaDataComponent(metaData));

        var tutorialSettings = metaData.TutorialSettings;
        
        tutorialSettings.MovementShown = progress.MovementShown;
        tutorialSettings.AttackingShown = progress.AttackingShown;
        tutorialSettings.DoorInteractionShown = progress.DoorInteractionShown;
        tutorialSettings.SnackMachineShown = progress.SnackMachineShown;
        tutorialSettings.BearTrapInteractionShown = progress.BearTrapInteractionShown;
        tutorialSettings.ChemicalTankInteractionShown = progress.ChemicalTankInteractionShown;
        tutorialSettings.TableInteractionShown = progress.TableInteractionShown;
        tutorialSettings.CorpseInteractionShown = progress.CorpseInteractionShown;
        tutorialSettings.MutantRoomShown = progress.MutantRoomShown;
        tutorialSettings.LastRoomPosition = progress.LastRoomPosition;

        metaData.TutorialSettings = tutorialSettings;

        SaveManager.SaveMeta(metaData);
    }
}