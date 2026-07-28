using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Components.Purchase;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Handles purchase requests: checks finances, subtracts coins, 
/// and sends an AddItemRequest to the InventorySystem.
/// </summary>
public class PurchaseSystem (AudioService audio) : IUpdateSystem
{
    private static readonly QueryDescription sRequestQuery = new QueryDescription()
        .WithAll<CoinsComponent, PurchaseRequestComponent>();
    
    private AudioService audioService = audio;

    public void Update(World world, GameTime gameTime)
    {
        var playerEntity = Entity.Null;
        world.Query(new QueryDescription().WithAll<InventoryComponent, PlayerTagComponent>(),
            (Entity entity) => { playerEntity = entity; });
        if (playerEntity == Entity.Null) { return; }

        var inventory = world.Get<InventoryComponent>(playerEntity);
        
        world.Query(in sRequestQuery,
            (Entity requestSource, ref CoinsComponent coins, ref PurchaseRequestComponent request) =>
            {
                var itemType = request.mItemType;
                var def = ItemDefinitions.Get(itemType);
                var cost = def.Price;

                if (coins.CurrentAmount < cost)
                {
                    audioService.PlaySfx("Sounds/UI/ErrorSound");
                    world.Remove<PurchaseRequestComponent>(requestSource);
                    return;
                }
                //sound confirm
                audioService.PlaySfx("Sounds/UI/PurchaseSound");
                
                //different for munition
                if (itemType == ItemType.Munition)
                {
                    coins.CurrentAmount -= cost;
                    var munition = world.Get<AmmoComponent>(playerEntity);
                    munition.Current += 10;
                    world.Get<AmmoComponent>(playerEntity) = munition;
                    world.Remove<PurchaseRequestComponent>(requestSource);
                    return;
                }
                
                bool canAdd;
                var hasType = InventorySystem.DoesInventoryContainType(world, inventory, itemType);
                bool hasFreeSlot = InventorySystem.HasFreeSlot(inventory);

                if (def.Stackable)
                {
                    canAdd = hasType || hasFreeSlot;
                }
                else
                {
                    canAdd = !hasType && hasFreeSlot;
                }

                // 4. Transaktion abschließen
                if (canAdd)
                {
                    coins.CurrentAmount -= cost;
                    world.Create(new AddItemRequestComponent(itemType));
                }

                world.Remove<PurchaseRequestComponent>(requestSource);
            });
    }
}