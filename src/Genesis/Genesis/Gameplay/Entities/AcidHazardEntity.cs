using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public static class AcidHazardEntity
{
    /// <summary>
    /// Creates an acid hazard entity from a Tiled map object.
    /// </summary>
    public static void Create(World world, TiledMapObject mapObject)
    {
        var size = new Vector2(mapObject.Size.Width, mapObject.Size.Height);
        var position = new Vector2(
            mapObject.Position.X + size.X / 2f,
            mapObject.Position.Y + size.Y / 2f
        );
        
        var gridMap = world.GetResource<GridMap>();
        gridMap?.AddDynamicWeight(position, mapObject.Size, 100);

        world.Create(
            new PositionComponent(position),
            new ColliderComponent(size),
            new AcidHazardComponent(),
            new TriggerColliderTagComponent()
        );
    }
}