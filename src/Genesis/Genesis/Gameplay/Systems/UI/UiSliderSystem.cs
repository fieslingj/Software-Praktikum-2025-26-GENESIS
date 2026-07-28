using System;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.UI;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems.UI;

public class UiSliderSystem(ScreenService screen) : IInputSystem
{
    private static QueryDescription sSliderQuery = new QueryDescription()
        .WithAll<UiSliderComponent, PositionComponent>();
    public void HandleInput(World world, InputService input)
    {
        var mousePosRaw = input.GetMousePosition();
        var mousePos = screen.Adapter.PointToScreen(mousePosRaw);
        var mousePressed = input.IsLeftMousePressed();
        var mouseDown = input.IsLeftMouseDown();

        world.Query(in sSliderQuery, (ref UiSliderComponent slider, ref PositionComponent pos) =>
        {
            // center the bounds around the position
            var bounds = slider.Bounds;
            bounds.X = (int)(pos.Value.X - bounds.Width / 2f);
            bounds.Y = (int)(pos.Value.Y - bounds.Height / 2f);
            
            var isInBounds = bounds.Contains(mousePos);
            UpdateState(ref slider, isInBounds, mousePressed, mouseDown);

            if (slider.State != UiSliderState.Dragging)
            {
                return;
            }

            var relativeX = mousePos.X - bounds.X;
            var t = MathHelper.Clamp(relativeX / (float)bounds.Width, 0f, 1f);
            var newValue = slider.Min + (t * (slider.Max - slider.Min));

            if (Math.Abs(slider.Value - newValue) < 0.001f) {return;}
            
            slider.Value = newValue;
            slider.OnSet?.Invoke(slider.Value);
        });
    }

    private void UpdateState(ref UiSliderComponent slider, bool mouseInBounds, bool mousePressed, bool mouseDown)
    {
        switch (slider.State)
        {
            case (UiSliderState.Idle):
                if (mouseInBounds) {slider.State = UiSliderState.Hover;}
                break;
                
            case (UiSliderState.Hover):
                if (!mouseInBounds) {slider.State = UiSliderState.Idle;}
                else if (mousePressed) {slider.State = UiSliderState.Dragging;}
                break;
                
            case (UiSliderState.Dragging):
                if (!mouseDown) {slider.State = mouseInBounds ? UiSliderState.Hover : UiSliderState.Idle;}
                break;
        }
    }
}