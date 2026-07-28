using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Components.Purchase;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Overlays;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Systems;

public class InteractionSystem(GameStateManager gameStateManager, AudioService audio) : IInputSystem
{
    private readonly QueryDescription mInteractableQuery = new QueryDescription()
        .WithAll<InteractableComponent>();

    private readonly QueryDescription mPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent>();

    public void HandleInput(World world, InputService input)
    {
        if (!input.IsActionPressed(InputAction.Interact)) { return; }

        var player = world.GetFirstEntity(in mPlayerQuery);
        if (player == Entity.Null) { return; }

        // Search for the target, which was already marked by the proximity light system. Prioritize corpses.
        var target = Entity.Null;
        var corpseFound = false;
        world.Query(in mInteractableQuery, (Entity entity, ref InteractableComponent interactable) =>
        {
            if (corpseFound || !interactable.LightOn) { return; }

            target = entity;
            corpseFound = interactable.Type is InteractionType.Corpse;
        });

        if (target == Entity.Null) { return; }

        TriggerInteraction(world, target, player);
    }

    private void TriggerInteraction(World world, Entity target, Entity player)
    {
        var interact = world.Get<InteractableComponent>(target);

        switch (interact.Type)
        {
            case InteractionType.Trap:
                DefuseTrap(world, target);
                break;

            case InteractionType.SnackMachine:
                gameStateManager.PushState(new PurchaseMenuState());
                break;

            case InteractionType.Corpse:
                LootCorpse(world, target, player);
                break;

            case InteractionType.Generic:
                break;

            case InteractionType.Door:
                OpenDoor(world, target);
                break;

            case InteractionType.ChemicalTank:
                ManipulateTank(world, target);
                break;

            case InteractionType.Table:
                TurnTable(world, target);
                break;

            case InteractionType.Elevator:
                OpenElevator(world,target);
                break;
        }
    }

    public static void DefuseTrap(World world, Entity trap)
    {
        var animation = new SimpleAnimationComponent(32, 32, 4, 4, .2f, isLooping: false);
        world.Add(trap, animation);

        ref var trapComponent = ref world.Get<TrapComponent>(trap);
        var position = world.Get<PositionComponent>(trap).Value;
        trapComponent.IsActive = false;

        var gridMap = world.GetResource<GridMap>();
        if (gridMap != null)
        {
            float avoidanceRadius = 32f;
            Vector2 areaSize = new Vector2(avoidanceRadius * 2);
            // They have to be the same values as in AddDynamicWeight!
            gridMap.RemoveDynamicWeight(position, areaSize, 250);
        }

        world.Remove<InteractableComponent>(trap);
    }

    private static void LootCorpse(World world, Entity corpse, Entity player)
    {
        var enemyType = world.Get<CorpseComponent>(corpse).Type;
        var def = EnemyDefinitions.Get(enemyType);

        // Loot inventory
        var inventory = def.Inventory;

        foreach (var (itemType, amount) in inventory)
        {
            world.Create(new AddItemRequestComponent(itemType, amount));
        }

        // Loot ammo
        world.Get<AmmoComponent>(player).Current += world.Get<AmmoComponent>(corpse).Current;

        // Loot coins
        world.Get<CoinsComponent>(player).CurrentAmount += def.Coins;

        world.Destroy(corpse);
    }

    private void OpenDoor(World world, Entity door)
    {
        ref var doorComponent = ref world.Get<DoorComponent>(door);
        ref var animation = ref world.Get<SimpleAnimationComponent>(door);

        // Can only open if no enemies remain
        if (!doorComponent.CanOpen) { return; }

        // Start opening animation
        doorComponent.State = DoorState.Opening;

        animation.CurrentFrame = 0; // Reset to start
        animation.IsFinished = false; // Allow animation to play

        //soundeffect
        audio.PlaySfx("Sounds/Effects/door_open");

        RemoveColliderAndUnblock(world, door);
        world.Remove<InteractableComponent>(door);
    }

    private void OpenElevator(World world, Entity elevator)
    {
        //only open when all visited are cleared
        var floor = world.GetResource<FloorLayoutComponent>();
        if (!FloorLayoutComponent.AllVisitedCleared(floor)) { return; }

        //animation
        SimpleAnimationComponent anime = new SimpleAnimationComponent(32,32,12,12,10,false);
        world.Add(elevator,anime);

        //soundeffect
        audio.PlaySfx("Sounds/Effects/elevator_ding");

        //remove collider
        RemoveColliderAndUnblock(world, elevator);
        world.Remove<InteractableComponent>(elevator);
    }

    private static void ManipulateTank(World world, Entity tank)
    {
        // Set tank to destroyed state
        ref var tankComponent = ref world.Get<ChemicalTankComponent>(tank);
        tankComponent.State = TankState.Destroyed;

        // Reset animation to play destruction
        ref var animation = ref world.Get<SimpleAnimationComponent>(tank);
        animation.CurrentFrame = 0;
        animation.FrameTimer = 0f;
        animation.IsFinished = false;
        RemoveColliderAndUnblock(world, tank);
        world.Remove<InteractableComponent>(tank);
    }

    private static void TurnTable(World world, Entity table)
    {
        ref var tableComponent = ref world.Get<TableComponent>(table);
        tableComponent.State = TableState.Flipped;
        world.Remove<InteractableComponent>(table);
    }
    
    /// <summary>
    /// Helper method to safely remove a collider and update the GridMap.
    /// </summary>
    private static void RemoveColliderAndUnblock(World world, Entity entity)
    {
        if (world.Has<ColliderComponent>(entity) && world.Has<PositionComponent>(entity))
        {
            var gridMap = world.GetResource<GridMap>();
            if (gridMap != null)
            {
                var pos = world.Get<PositionComponent>(entity).Value;
                var col = world.Get<ColliderComponent>(entity);
                
                gridMap.SetWalkability(pos, col, true);
            }
        }
        
        world.Remove<ColliderComponent>(entity);
    }
}