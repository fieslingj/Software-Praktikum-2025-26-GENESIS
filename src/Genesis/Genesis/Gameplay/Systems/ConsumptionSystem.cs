using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Extensions;
using Genesis.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Handles consumption requests: checks if a consumable item is in the active slot.
/// Handles Weapons (attacks) and Consumables (health/stamina).
/// </summary>
public class ConsumptionSystem(ContentManager content,FactoryService factoryService, CameraService camera, ScreenService screen, AudioService audioService) : IInputSystem
{
    private static readonly QueryDescription sRequestQuery = new QueryDescription()
        .WithAll<HotbarComponent, PlayerTagComponent>();

    private static readonly QueryDescription sEnemyQuery = new QueryDescription()
        .WithAll<EnemyComponent, PositionComponent, HealthComponent, HitBoxComponent, StateComponent>();

    private SoundEffect mNoAmmoSound = content.Load<SoundEffect>("Sounds/Attack/NoAmmoSound");
    
    public void HandleInput(World world, InputService input)
    {
        if (input.IsActionDown(InputAction.PrimaryItemAction))
        {
            // for consumables
            var isPressed = input.IsActionPressed(InputAction.PrimaryItemAction);
            
            var rawMousePos = input.GetMousePosition();

            var virtualMousePoint = screen.Adapter.PointToScreen(rawMousePos.X, rawMousePos.Y);
            var uiMousePos = virtualMousePoint.ToVector2();

            var isInsideGameArea = uiMousePos.X is >= 0 and <= ScreenService.VirtualWidth &&
                                   uiMousePos.Y is >= 0 and <= ScreenService.VirtualHeight;

            if (isInsideGameArea && !HudWorld.IsMouseOverGui(uiMousePos)) 
            {
                var worldMousePos = camera.ScreenToWorld(rawMousePos);
                ProcessPrimaryAction(world, worldMousePos, isPressed);
            }
        }

        if (input.IsActionPressed(InputAction.SecondaryItemAction))
        {
            ProcessSecondaryAction(world);
        }
    }

    private bool UseItem(World world, ItemType itemType, Vector2 mousePos, Entity player, Entity itemEntity,bool isPressed)
    {
        var def = ItemDefinitions.Get(itemType);

        if (def.IsConsumable)
        {
            if (isPressed)
            {
                ApplyConsumableEffects(world, player, def);
                return true;
            }
            return false;
        }
        else
        {
            //sonst wirft man aufeinmal alles
            if (!isPressed && def.Stackable == true) return false;
            return HandleActionItem(world, player, itemEntity, itemType, mousePos);
        }
    }

    private void ApplyConsumableEffects(World world, Entity player, ItemDefinition def)
    {
        if (def.HealthRestore > 0 && world.Has<HealthComponent>(player))
        {
            ref var health = ref world.Get<HealthComponent>(player);
            health.Current = Math.Min(health.Max, health.Current + def.HealthRestore);
            audioService.PlaySfx("Sounds/Effects/healthup");
        }

        if (def.StaminaRestore > 0 && world.Has<StaminaComponent>(player))
        {
            ref var stamina = ref world.Get<StaminaComponent>(player);
            stamina.Current = Math.Min(stamina.Max, stamina.Current + def.StaminaRestore);
            audioService.PlaySfx("Sounds/Effects/staminaup");
        }
    }

    // Try to use an action item. If false, the item should not be removed from the inventory.
    private bool HandleActionItem(World world, Entity player, Entity itemEntity, ItemType type, Vector2 mousePos)
    {
        var playerPos = world.Get<PositionComponent>(player).Value;
        var def = ItemDefinitions.Get(type);

        // Weapons
        // Note: Minigun has stats but is placed as a turret, so we exclude it here to handle it in the switch below.
        if (def.AttackType != ItemAttackType.None && type != ItemType.Minigun)
        {
            bool attackSuccess = HandleWeapon(world, player, def, mousePos);
            if (!attackSuccess) { return false; }

            // If throwable, the item should be removed, otherwise not
            return type is ItemType.StunGrenade or ItemType.RemoteExplosive;
        }

        // Other action items
        switch (type)
        {
            case ItemType.Minigun:
                factoryService.MMinigunFactory.Create(world, playerPos);
                audioService.PlaySfx("Sounds/Effects/setturret");
                return true;

            case ItemType.Shield:
                if (world.Has<ActiveShieldComponent>(player)) { return false; }
                world.Add(player, new ActiveShieldComponent());
                audioService.PlaySfx("Sounds/Effects/shieldup");
                return false;
            case ItemType.Neurochip:
                return TryApplyNeurochip(world, mousePos, player);
            case ItemType.AcidSpitSyringe or ItemType.ArmsOfSteelSyringe or ItemType.LaserArmSyringe:
                ConvertSyringeToAbility(world, itemEntity);
                return false;
        }
        return false;
    }
    
    // Return true if an attack was performed (cooldown)
    private bool HandleWeapon(World world, Entity player, ItemDefinition def, Vector2 mouseWorldPos)
    {
        var aimSourcePos = world.GetCenter(player);
        var direction = mouseWorldPos - aimSourcePos;
        
        // Delegate the attack logic (Melee, Ranged, RangedAOE) to the extension method.
        // This keeps the system clean and prevents code duplication.
        var attackSuccessful = world.UseWeapon(
            owner: player,
            weaponDefinition: def,
            targetDirection: direction,
            factory: factoryService,
            audioService
        );
        
        // Set the state to attacking
        if (!attackSuccessful)
        { return false; }
        ref var state = ref world.Get<StateComponent>(player);
        if (state.Current != ActorState.Hit)
        {
            state.Previous = state.Current;
            state.Current = ActorState.Attacking;
        }

        return true;
    }

    private void DetonatePlacements(World world)
    {
        // Search for the placed remote explosives.
        var placedQuery = new QueryDescription().WithAll<RemoteExplosiveComponent, PositionComponent>();
        var def = ItemDefinitions.Get(ItemType.RemoteExplosive);
        
        List<Entity> toDestroy = [];
        world.Query(in placedQuery, (Entity entity, ref PositionComponent pos) =>
        {
            factoryService.MExplosivesFactory.DetonateRemoteExplosive(world, pos.Value);
            toDestroy.Add(entity);
        });

        // Remove all detonated entities
        foreach (var e in toDestroy)
        {
            world.Destroy(e);
        }
    }
    
    private void ProcessPrimaryAction(World world, Vector2 mousePos, bool isPressed)
    {
        world.Query(sRequestQuery,
            (Entity player, ref HotbarComponent hotbar) =>
            {
                var itemEntity = hotbar.Slots[hotbar.ActiveSlot];
                if (itemEntity == Entity.Null || !world.IsAlive(itemEntity)) {return;}

                ref var id = ref world.Get<ItemIdentificationComponent>(itemEntity);
                if (world.Has<ItemStackComponent>(itemEntity))
                {
                    ref var stack = ref world.Get<ItemStackComponent>(itemEntity);
                    if (stack.mCount <= 0) { return; }
                }

                if (UseItem(world, id.mType, mousePos, player, itemEntity, isPressed))
                {
                    world.Create(new RemoveItemRequestComponent(id.mType));
                }
            });
    }
    
    private void ProcessSecondaryAction(World world)
    {
        world.Query(sRequestQuery, (ref HotbarComponent hotbar) =>
        {
            var itemEntity = hotbar.Slots[hotbar.ActiveSlot];
            if (itemEntity == Entity.Null || !world.IsAlive(itemEntity)) {return;}

            ref var id = ref world.Get<ItemIdentificationComponent>(itemEntity);
            if (id.mType != ItemType.RemoteExplosive) { return; }
            
            ref var stack = ref world.Get<ItemStackComponent>(itemEntity);

            DetonatePlacements(world);
            if (stack.mCount <= 0)
            {
                world.Create(new ClearEmptySlotRequestComponent(id.mType));
            }
        });
    }

    private bool TryApplyNeurochip(World world, Vector2 mouseWorldPos, Entity player)
    {
        const float playerReach = 80f;

        var targetEnemy = Entity.Null;
        var rayOrigin = world.GetCenter(player);
        var aimDirection = mouseWorldPos - rayOrigin;
        if (aimDirection != Vector2.Zero) {aimDirection.Normalize();}

        // Search for an enemy in aim direction within reach
        world.Query(in sEnemyQuery,
            (Entity entity,
                ref EnemyComponent enemy,
                ref HealthComponent health,
                ref PositionComponent enemyPos,
                ref HitBoxComponent hitbox,
                ref StateComponent state) =>
            {
                if (targetEnemy != Entity.Null){ return; }

                // Only applicable for mutants
                if (!EnemyDefinitions.Get(enemy.Type).IsMutant) { return; }
                
                var enemyCenter = world.GetCenter(entity);
                var distToPlayer = Vector2.Distance(enemyCenter, rayOrigin);
                if (distToPlayer > playerReach) {return;}
                
                var enemyRect = hitbox.GetBounds(enemyPos.Value);

                if (!RayIntersectsRect(rayOrigin, aimDirection, enemyRect, playerReach)) { return; }

                // Conditions: Low Health & Stunned
                var vulnerable = health.Current < 0.4f * health.Max;
                var isStunned = state.Current == ActorState.Stunned;

                if (vulnerable && isStunned)
                {
                    targetEnemy = entity;
                }
            });

        if (targetEnemy == Entity.Null) { return false; }

        // Pass factoryService instance to the static method
        ConvertEnemyToCompanion(world, targetEnemy, factoryService);
        audioService.PlaySfx("Sounds/Effects/setneurachip");
        return true;
    }
    
    private static bool RayIntersectsRect(Vector2 rayOrigin, Vector2 rayDir, Rectangle rect, float maxDistance)
    {
        float minX = rect.Left;
        float maxX = rect.Right;
        float minY = rect.Top;
        float maxY = rect.Bottom;

        var t1 = (minX - rayOrigin.X) / rayDir.X;
        var t2 = (maxX - rayOrigin.X) / rayDir.X;
        var t3 = (minY - rayOrigin.Y) / rayDir.Y;
        var t4 = (maxY - rayOrigin.Y) / rayDir.Y;

        var tmin = Math.Max(Math.Min(t1, t2), Math.Min(t3, t4));
        var tmax = Math.Min(Math.Max(t1, t2), Math.Max(t3, t4));

        if (tmax < 0) {return false;}
        if (tmin > tmax) {return false;}
        return tmin <= maxDistance;
    }
    
    private static void ConvertEnemyToCompanion(World world, Entity target, FactoryService factory)
    {
        var type = world.Get<EnemyComponent>(target).Type;
        world.Remove<EnemyComponent>(target);
        world.Add(target, new CompanionComponent(type)); 
    
        ref var health = ref world.Get<HealthComponent>(target);
        health.Current = health.Max;

        // Spawn heart animation above the new companion
        factory.MEffectFactory.CreateCompanionHeart(world, target);
    }

    private void ConvertSyringeToAbility(World world, Entity itemEntity)
    {
        ref var id = ref world.Get<ItemIdentificationComponent>(itemEntity);
        switch (id.mType)
        {
            case ItemType.AcidSpitSyringe:
                id.mType = ItemType.AcidSpit;
                break;
            case ItemType.ArmsOfSteelSyringe:
                id.mType = ItemType.ArmsOfSteel;
                break;
            case ItemType.LaserArmSyringe:
                id.mType = ItemType.LaserArm;
                break;
        }
        world.Add(itemEntity, new LifeTimeComponent(60, true));
    }
}