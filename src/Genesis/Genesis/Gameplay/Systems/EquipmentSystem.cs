using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Manage the equipment: For example, deactivate the shield, if not equipped anymore.
/// </summary>
public class EquipmentSystem : IUpdateSystem
{
    private static readonly QueryDescription sPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent, HotbarComponent>();

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sPlayerQuery, (Entity entity, ref HotbarComponent hotbar) => 
        {
            if (!world.Has<ActiveShieldComponent>(entity)) { return; }
            
            var heldItemType = ItemType.None;
            var itemEntity = hotbar.Slots[hotbar.ActiveSlot];
            
            if (itemEntity != Entity.Null && world.IsAlive(itemEntity))
            {
                heldItemType = world.Get<ItemIdentificationComponent>(itemEntity).mType;
            }

            if (heldItemType != ItemType.Shield)
            {
                world.Remove<ActiveShieldComponent>(entity);
            }
        });
    }
}