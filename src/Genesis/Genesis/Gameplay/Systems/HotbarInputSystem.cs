using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Components;

namespace Genesis.Gameplay.Systems;

public class HotbarInputSystem : IInputSystem
{
    private static readonly QueryDescription sPlayerQuery = new QueryDescription()
        .WithAll<HotbarComponent, PlayerTagComponent>();

    public void HandleInput(World world, InputService input)
    {
        world.Query(sPlayerQuery,
            (ref HotbarComponent hotbar) =>
            {

                if (input.IsActionPressed(InputAction.ActivateSlot0))
                {
                    hotbar.ActiveSlot = 0;
                }
                else if (input.IsActionPressed(InputAction.ActivateSlot1))
                {
                    hotbar.ActiveSlot = 1;
                }
                else if (input.IsActionPressed(InputAction.ActivateSlot2))
                {
                    hotbar.ActiveSlot = 2;
                }
                else if (input.IsActionPressed(InputAction.ActivateSlot3))
                {
                    hotbar.ActiveSlot = 3;
                }
                else if (input.IsActionPressed(InputAction.ActivateSlot4))
                {
                    hotbar.ActiveSlot = 4;
                }

                int scrollDelta = input.GetMouseScroll();
                if (scrollDelta != 0)
                {
                    // Scroll up to go to the left slot, scroll down to go to the right slot
                    int direction = scrollDelta > 0 ? -1 : 1;
                    int maxSlots = HotbarComponent.SlotCount;
                    int nextSlot = hotbar.ActiveSlot + direction;

                    hotbar.ActiveSlot = (nextSlot + maxSlots) % maxSlots;
                }
            });
    }
}