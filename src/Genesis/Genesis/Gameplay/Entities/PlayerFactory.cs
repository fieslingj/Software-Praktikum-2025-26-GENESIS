using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Components.Purchase;
using Genesis.Gameplay.Definitions;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Entities;

public class PlayerFactory(ContentManager content)
{
    private const int StartingCoins = 0;
    private const int StartingAmmo = 0;
    private const float AnimationFrameDuration = 100f;
    private const int FrameSize = 32;
    private const float LayerDepth = 0.1f;

    /// <summary>
    /// Creates a player entity in the given world at the specified position.
    /// </summary>
    public Entity CreateNew(World world, Vector2 position, MutantType mutant)
    {
        var def = PlayerDefinitions.Get(mutant);
        
        var playerData = new SavedPlayerData()
        {
            Position = new PositionComponent(position),
            Health = new HealthComponent(def.MaxHealth),
            MutantType = new MutantTypeComponent(mutant),
            Stamina = new StaminaComponent(def.MaxStamina),
            Mass = new MassComponent(def.Mass),
            Coins = new CoinsComponent(StartingCoins),
            Ammo = new AmmoComponent(StartingAmmo),
            Inventory = new SavedInventoryData()
            {
                InventoryMaxSize = 15,
                Items = new Dictionary<ItemType, SavedItemProperties>
                {
                    {ItemType.Pipe, new SavedItemProperties(1)},
                    {def.SpecialItem, new SavedItemProperties(1)}
                },
                HasHotbar = true,
                ActiveSlot = 0,
                Hotbar = [ItemType.Pipe, def.SpecialItem],
            },
            BloodlustTracker = new BloodlustTrackerComponent(),
        };

        return Create(world, playerData);
    }

    public Entity Recreate(World world, SavedPlayerData playerData) => Create(world, playerData);

    private Entity Create(World world, SavedPlayerData playerData)
    {
        var def = PlayerDefinitions.Get(playerData.MutantType.Type);
        var texture = content.Load<Texture2D>(def.SpritePath);
        var sourceRect = new Rectangle(0, 0, FrameSize, FrameSize);
     
        var mutantDef = PlayerDefinitions.GetMutantEnemyDefinition(playerData.MutantType.Type);
        var colliderSize = FrameSize * def.Scale * mutantDef.ColliderSizeFactor;
        var hitboxSize = new Vector2(colliderSize.X, FrameSize * def.Scale * mutantDef.HitboxHeightFactor);
        var hitboxOffset = new Vector2(0, (colliderSize.Y - hitboxSize.Y) / 2f);
        
        var playerEntity = world.Create(
            new PlayerTagComponent(),
            playerData.MutantType,
            playerData.Position,
            playerData.Health,
            playerData.Stamina,
            playerData.Mass,
            playerData.Coins,
            playerData.Ammo,
            playerData.BloodlustTracker,
            new VelocityComponent(Vector2.Zero, def.MovementSpeed),
            new SimpleAnimationComponent(FrameSize, FrameSize, AnimationFrameDuration, 4, 4),
            new SpriteComponent(texture, sourceRect, LayerDepth, def.Scale)
            {
                Origin = new Vector2(FrameSize / 2, FrameSize / 2) + FrameSize * mutantDef.SpriteOriginOffsetFactor
            },
            new ColliderComponent(colliderSize),
            new HitBoxComponent(hitboxSize, hitboxOffset),
            new InventoryComponent(playerData.Inventory.InventoryMaxSize),
            new HotbarComponent(playerData.Inventory.ActiveSlot),
            new AttackCooldownComponent(),
            new DuckBehaviorComponent(probability: 1f, duration: 999f, reactionRange: 0f),

            // Add the StateComponent.
            // Set both to idle, the player is not moving in the beginning.
            // This will be updated by PlayerInputSystem and used by other systems
            // (e.g. MovementSoundSystem).
            new StateComponent
                {
                    Current = ActorState.Idle,
                    Previous = ActorState.Idle
                },

            new MovementSoundComponent
            {
                WalkSoundInstance = null,
                SprintSoundInstance = null,
                WalkSoundPath = "Sounds/Moving/PlayerWalkingSoundLoop",
                SprintSoundPath = "Sounds/Moving/PlayerSprintingSoundLoop"
            },
            new HitSoundComponent("Sounds/Attack/GegnerHitTestSound"),
            new FaceComponent(FaceDirection.North),
            new StatusComponent([]),
            new CompanionSelectionComponent()
        );

        foreach (var (itemType, properties) in playerData.Inventory.Items.Reverse())
        {
            int? hotbarSlot = null;
            for (var i = 0; i < playerData.Inventory.Hotbar.Length; i++)
            {
                if (playerData.Inventory.Hotbar[i] == itemType) { hotbarSlot = i; }
            }
            
            world.Create(new AddItemRequestComponent(itemType, properties, hotbarSlot));
        }

        return playerEntity;
    }
}