using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class MinigunControlSystem(FactoryService factories, AudioService audioService) : IUpdateSystem
{
    private static readonly QueryDescription sMinigunQuery = new QueryDescription()
        .WithAll<MinigunTagComponent, PositionComponent, LoadoutComponent, AttackCooldownComponent, AmmoComponent>();

    private static readonly QueryDescription sTargetQuery = new QueryDescription()
        .WithAll<EnemyComponent, PositionComponent, HealthComponent>();
    
    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sMinigunQuery, (Entity minigun, ref PositionComponent pos, ref LoadoutComponent loadout) =>
        {
            if (!loadout.HasRanged) { return; }
            var range = loadout.Ranged.AttackRange;
            var minDistanceSq = range * range;
            
            var closestTarget = Entity.Null;
            var minigunPos = pos.Value;

            // Find the closest target
            world.Query(in sTargetQuery, (Entity targetEntity, ref PositionComponent tPos) =>
            {
                var distSq = Vector2.DistanceSquared(minigunPos, tPos.Value);
                if (!(distSq < minDistanceSq)) { return; }

                minDistanceSq = distSq;
                closestTarget = targetEntity;
            });

            if (closestTarget == Entity.Null) { return; }

            var targetPos = world.Get<PositionComponent>(closestTarget).Value;
            var direction = targetPos - pos.Value;

            world.UseWeapon(
                minigun, 
                loadout.Ranged,
                direction,
                factories,
                audioService
            );
        });
    }
}