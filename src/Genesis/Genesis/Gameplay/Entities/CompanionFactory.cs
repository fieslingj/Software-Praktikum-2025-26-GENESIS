using Microsoft.Xna.Framework;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Entities;

/// <summary>
/// Factory class to create companion entities with specified attributes and components.
/// </summary>
public class CompanionFactory(ContentManager content) : CharacterFactory(content)
{
    private readonly EffectFactory mEffectFactory = new(content);
    
    public Entity Create(World world, Vector2 position, EnemyType type)
    {
        var def = EnemyDefinitions.Get(type);
        var common = CreateCommonComponents(def);
        
        var entity = world.Create(
            new CompanionComponent(type),
            new PositionComponent(position),
            new HealthComponent(def.MaxHealth),
            new MassComponent(def.Mass),
            new AmmoComponent(def.InitialAmmo),
            common.Velocity,
            common.State,
            common.Sprite,
            common.Animation,
            common.Collider,
            common.HitBox,
            common.Loadout,
            common.HitSound,
            common.AttackCooldown,
            common.Status
        );
        
        AddOptionalBehaviors(world, entity, def);

        mEffectFactory.CreateCompanionHeart(world, entity);
        return entity;
    }
    
    /// <summary>
    /// Recreate a companion from SavedCompanionData.
    /// </summary>
    public Entity Recreate(World world, SavedCompanionData companionData)
    {
        var def = EnemyDefinitions.Get(companionData.Type.Type);
        var common = CreateCommonComponents(def);

        var entity = world.Create(
            companionData.Type,
            companionData.Position,
            companionData.Health,
            companionData.Ammo,
            common.Velocity,
            common.State,
            common.Sprite,
            common.Animation,
            common.Collider,
            common.HitBox,
            common.Loadout,
            common.HitSound,
            common.AttackCooldown,
            common.Status
        );
        
        AddOptionalBehaviors(world, entity, def);

        mEffectFactory.CreateCompanionHeart(world, entity);
        return entity;
    }
}