using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public static class WallEntity
{
    /// <summary>
    /// Creates a vertical wall entity from a Tiled map object.
    /// </summary>
    public static Entity Create(World world, TiledMapObject mapObject, Texture2D doorTexture)
    {
        var size = new Vector2(mapObject.Size.Width, mapObject.Size.Height);
        var offset = new Vector2(size.X / 2, size.Y - 4);
        var position = mapObject.Position + offset;

        // sprite setup
        const int frameWidth = 32;
        const int frameHeight = 32;
        const int colliderHeight = 8;
        const int startX = 160;
        const int startY = 320;
        var sourceRect = new Rectangle(startX, startY, frameWidth, frameHeight);

        var spriteComponent = new SpriteComponent(
            doorTexture, 
            sourceRect, 
            layerDepth: 0.1f,
            scale: 1.0f
        ) { Origin = offset};
        
        var collider = new ColliderComponent(
            new Vector2(frameWidth, colliderHeight)
        );
        var gridMap = world.GetResource<GridMap>();
        gridMap?.MarkColliderAsUnwalkable(position, collider);

        var entity = world.Create(
            new PositionComponent(position),
            collider,
            spriteComponent
        );

        return entity;
    }
}