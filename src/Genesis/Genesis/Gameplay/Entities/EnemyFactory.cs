using Microsoft.Xna.Framework;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Entities;

/// <summary>
/// Factory class to create enemy entities with specified attributes and components.
/// </summary>
public class EnemyFactory(ContentManager content) : CharacterFactory(content)
{
    public Entity Create(World world, Vector2 position, EnemyType type)
    {
        var def = EnemyDefinitions.Get(type);
        var common = CreateCommonComponents(def);
        
        var entity = world.Create(
            new EnemyComponent(type),
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

        return entity;
    }
    
    /// <summary>
    /// Recreate an enemy from SavedEnemyData.
    /// </summary>
    public Entity Recreate(World world, SavedEnemyData enemyData)
    {
        var def = EnemyDefinitions.Get(enemyData.Type.Type);
        var common = CreateCommonComponents(def);

        var entity = world.Create(
            enemyData.Type,
            enemyData.Position,
            enemyData.Health,
            enemyData.Ammo,
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

        return entity;
    }
}