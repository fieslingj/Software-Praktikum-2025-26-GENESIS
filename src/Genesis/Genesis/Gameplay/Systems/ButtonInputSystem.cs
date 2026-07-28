using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;

namespace Genesis.Gameplay.Systems;

public class ButtonInputSystem(ScreenService screen) : IInputSystem
{
    // Query description to find all button entities
    private static readonly QueryDescription sQueryDesc = new QueryDescription()
        .WithAll<PositionComponent, ButtonComponent>();

    public void HandleInput(World world, InputService input)
    {
        // check if left mouse button was just clicked
        var wasJustClicked = input.IsLeftMousePressed();

        world.Query(in sQueryDesc,
            (ref PositionComponent pos, ref ButtonComponent button) =>
            {
                button.IsPressed = false;
                // calculate button bounds
                var buttonBounds = button.Bounds;

                // center the bounds around the position
                buttonBounds.X = (int)(pos.Value.X - buttonBounds.Width / 2f);
                buttonBounds.Y = (int)(pos.Value.Y - buttonBounds.Height / 2f);

                var physicalMousePosition = input.GetMousePosition();
                var virtualMousePosition = screen.Adapter.PointToScreen(
                    (int)physicalMousePosition.X,
                    (int)physicalMousePosition.Y
                );
                
                // check if mouse is over the button
                var isOverButton = buttonBounds.Contains(virtualMousePosition);

                // update hover state
                button.IsHovered = isOverButton;

                // return, unless clicked
                if (!(isOverButton && wasJustClicked)) {return;}
                
                // run the button's click action
                button.OnClick.Invoke();

                // change sprite to pressed state (to be implemented)
                button.IsPressed = true; 
            });
    }
}