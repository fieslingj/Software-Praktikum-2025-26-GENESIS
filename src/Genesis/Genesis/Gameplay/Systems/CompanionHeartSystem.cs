using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class CompanionHeartSystem : IUpdateSystem
{
    private static readonly QueryDescription sHeartQuery = new QueryDescription()
        .WithAll<CompanionHeartComponent, PositionComponent>();

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sHeartQuery, (Entity heartEntity, ref CompanionHeartComponent heart, ref PositionComponent heartPos) =>
        {
            // If the companion is dead or gone, destroy the heart
            if (!world.IsAlive(heart.Owner))
            {
                world.Destroy(heartEntity);
                return;
            }

            // Get the owner's position
            var ownerPos = world.Get<PositionComponent>(heart.Owner).Value;

            // Offset the heart to float above the companion (e.g., 30 pixels up)
            heartPos.Value = ownerPos + new Vector2(0, -30f);
        });
    }
}