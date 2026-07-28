using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public static class CollisionEntity
{
    public static Entity Create(World world, Vector2 position, Vector2 size)
    {
        var collider = new ColliderComponent(size);

        // 2. GridMap aus der World holen und aktualisieren
        var gridMap = world.GetResource<GridMap>();
        gridMap?.MarkColliderAsUnwalkable(position, collider);

        return world.Create(
            new PositionComponent(position),
            collider
        );
    }
    
    public static Entity Create(World world, MonoGame.Extended.RectangleF rect)
    {
        var centerPosition = new Vector2(
            rect.X + rect.Width / 2f,
            rect.Y + rect.Height / 2f
        );
        return Create(world, centerPosition, new Vector2(rect.Width, rect.Height));
    }
}