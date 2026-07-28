using Genesis.Architecture.ECS; 
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class LifeTimeSystem : IUpdateSystem
{
    private static readonly QueryDescription sSLifeTimeQuery = new QueryDescription()
        .WithAll<LifeTimeComponent>();
    
    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sSLifeTimeQuery, (Entity entity, ref LifeTimeComponent life) =>
        {
            if (!life.Active)
            {
                return;
            }
            //entity entfernen bei Lebensende
            life.RemainingLifeTimeSeconds -= gameTime.ElapsedGameTime.TotalSeconds;

            if (!(life.RemainingLifeTimeSeconds <= 0)) { return; }

            if (world.Has<ItemIdentificationComponent>(entity))
            {
                var type = world.Get<ItemIdentificationComponent>(entity).mType;
                world.Create(new ClearEmptySlotRequestComponent(type));
            }
            else
            {
                world.Destroy(entity);
            }
        });
    }
}