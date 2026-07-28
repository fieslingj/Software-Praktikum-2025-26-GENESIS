using System;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Navigation;
using Genesis.Gameplay.Extensions;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Entities;

/// <summary>
/// Factory to create traps dealing damage and playing an effect when triggered
/// </summary>
/// <param name="content"> für sprite</param>
public class TrapFactory(ContentManager content)
{
    private const float DisarmRadius = 50f;
    private const float LayerDepth = 0.11f;
    private const float Scale = 0.9f;

    private Texture2D BearTrapTexture { get; } = content.Load<Texture2D>("Sprites/Traps/Bear_Trap");

    public Entity Create(World world, Vector2 position, TrapType type, float damage, Vector2 effectPosition, bool isFromTiled = false)
    {
        var data = new SavedTrapData
        {
            Type = type,
            Damage = damage,
            Radius = 20f,
            IsActive = true,
            Position = new PositionComponent(position),
            EffectPosition = effectPosition
        };

        return Create(world, data, isFromTiled);
    }

    public Entity Recreate(World world, SavedTrapData data)
    {
        return Create(world, data, false);
    }

    private Entity Create(World world, SavedTrapData data, bool isFromTiled)
    {
        return data.Type switch
        {
            TrapType.Bomb => CreateBearTrap(world, data, isFromTiled),
            _ => throw new ArgumentOutOfRangeException(nameof(data.Type), data.Type, null)
        };
    }

    private Entity CreateBearTrap(World world, SavedTrapData data, bool isFromTiled)
    {
        const int frameSize = 32;
        const int textureDisarmedX = 3 * frameSize;

        var trapPosition = data.Position.Value;

        // Offset position when loaded from Tiled
        if (isFromTiled) { trapPosition -= new Vector2(-frameSize / 2f, frameSize / 2f); }

        var sourceX = data.IsActive ? 0 : textureDisarmedX;
        var sourceRect = new Rectangle(sourceX, 0, frameSize, frameSize);

        // Prepare the triggered effect
        var effectFactory = new EffectFactory(content);
        var effect = effectFactory.Create(
            world,
            data.EffectPosition,
            EffectType.ExplosionAnimation,
            isFromTiled
        );

        // Prepare the trap
        var trapComponent = new TrapComponent(data.Type, data.Damage, effect, data.Radius);
        trapComponent.IsActive = data.IsActive;
        var trap = world.Create(
            new PositionComponent(trapPosition),
            new SpriteComponent(BearTrapTexture, sourceRect, LayerDepth, Scale),
            trapComponent
        );

        // If disarmed, return the non-dangerous trap
        if (!data.IsActive) {return trap;}

        var gridMap = world.GetResource<GridMap>();
        if (gridMap != null)
        {
            float avoidanceRadius = 48f;
            Vector2 areaSize = new Vector2(avoidanceRadius);
            // They have to be the same values as in RemoveDynamicWeight!
            gridMap.AddDynamicWeight(trapPosition, areaSize, 250);
        }

        // If not disarmed, traps are interactable and dangerous
        world.Add(trap, new InteractableComponent(DisarmRadius, InteractionType.Trap));

        return trap;
    }
}