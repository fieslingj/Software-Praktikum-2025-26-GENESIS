using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems.Render;

/// <summary>
/// System responsible for Frustum culling.
/// It determines which entities are within the camera's view and marks them.
/// Optimizes rendering and logic updates.
/// </summary>
public class CullingSystem(CameraService cameraService) : IUpdateSystem
{
    // We want to look at everything that is positioned somewhere and has visual representation.
    private static readonly QueryDescription sCullableQuery = new QueryDescription()
        .WithAll<PositionComponent, SpriteComponent>()
        .WithNone<IgnoreCullingComponent>();

    public void Update(World world, GameTime gameTime)
    {
        if (cameraService.ActiveCamera == null) { return; }

        // We use a margin, so that objects do not simply pop up.
        var bounds = cameraService.ActiveCamera.BoundingRectangle;
        const float margin = 64f;

        // Calculate the expanded boundary (including the margin) for the visibility check.
        float minX = bounds.Left - margin;
        float maxX = bounds.Right + margin;
        float minY = bounds.Top - margin;
        float maxY = bounds.Bottom + margin;

        world.Query(in sCullableQuery, (Entity entity, ref PositionComponent pos, ref SpriteComponent sprite) =>
        {
            bool isVisible = pos.Value.X >= minX && pos.Value.X <= maxX &&
                pos.Value.Y >= minY && pos.Value.Y <= maxY;

            bool hasVisibleTag = world.Has<IsVisibleComponent>(entity);

            if (isVisible && !hasVisibleTag)
            {
                world.Add(entity, new IsVisibleComponent());
            }
            else if (!isVisible && hasVisibleTag)
            {
                world.Remove<IsVisibleComponent>(entity);
            }
        });
    }
}