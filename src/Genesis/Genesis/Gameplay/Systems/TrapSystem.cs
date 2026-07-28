using System;
using Genesis.Architecture.ECS;
using Arch.Core;
using Arch.Core.Extensions;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class TrapSystem(AudioService audio) : IUpdateSystem
{
    private static readonly QueryDescription sEnemyOrPlayerQuery = new QueryDescription()
        .WithAll<PositionComponent, HealthComponent>().WithAny<EnemyComponent, PlayerTagComponent>();

    private static readonly QueryDescription sTrapQuery = new QueryDescription()
        .WithAll<PositionComponent, TrapComponent>();

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sEnemyOrPlayerQuery, (Entity e, ref PositionComponent pos) =>
        {
            var entityPos = pos.Value;
            world.Query(in sTrapQuery, (Entity trapEntity, ref TrapComponent trap, ref PositionComponent trapPos) =>
            {
                if (!trap.IsActive || Vector2.Distance(entityPos, trapPos.Value) > trap.Radius) { return; }

                switch (trap.Type)
                {
                    case TrapType.Bomb:
                        TriggerBomb(world, e, trapEntity, ref trap);
                        break;

                    default:
                        throw new ArgumentException("Trap type not supported!");
                }
            });
        });

    }

    private void TriggerBomb(World world, Entity targetEntity, Entity trapEntity, ref TrapComponent trap)
    {
        var gridMap = world.GetResource<GridMap>();
        if (gridMap != null)
        {
            float avoidanceRadius = 32f;
            Vector2 areaSize = new Vector2(avoidanceRadius * 2);
            var position = world.Get<PositionComponent>(trapEntity).Value;
            // They have to be the same values as in AddDynamicWeight!
            gridMap.RemoveDynamicWeight(position, areaSize, 250);
        }

        DamagePayload payload = new(trap.Damage, trapEntity);
        world.InflictDamage(targetEntity, payload, world.GetCurrentRunTimeSeconds());
        world.ShakeLarge();

        // Activate effect
        audio.PlaySfx("Sounds/Effects/ExplosionSound");
        if (world.IsAlive(trap.EffectEntity))
        {
            trap.EffectEntity.Get<LifeTimeComponent>().Active = true;
            trap.EffectEntity.Get<EffectComponent>().Active = true;
        }
        world.Destroy(trapEntity);
    }
}