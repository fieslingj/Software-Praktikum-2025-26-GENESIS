using System;
using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class CooldownSystem : IUpdateSystem
{
    private static readonly QueryDescription sQueryDesc = new QueryDescription()
        .WithAll<AttackCooldownComponent>();

    public void Update(World world, GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        world.Query(in sQueryDesc, (ref AttackCooldownComponent cooldown) =>
        {
            cooldown.CurrentTime = Math.Min(cooldown.CurrentTime += deltaTime, cooldown.Delay);
        });
    }
}