using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Entities;

public static class SnackMachineEntity
{
    private const int TileLid = 251;
    private const int TileColumns = 24;

    /// <summary>
    /// Creates a snack machine entity with position, collider, sprite and proximity light components.
    /// </summary>
    public static void Create(
        World world,
        Vector2 tilePosition,
        Texture2D spriteSheet,
        GridMap gridMap)
    {
        var position = tilePosition + new Vector2(16, 26);
        var sourceRect = GetSourceRect(32, 32);
        var spriteOrigin = new Vector2(16, 26);
        var spriteComponent = new SpriteComponent(spriteSheet, sourceRect, 0.1f, scale: 1.0f) { Origin = spriteOrigin };
        var colliderSize = new Vector2(22, 12);
        var collider = new ColliderComponent(colliderSize);
        gridMap.MarkColliderAsUnwalkable(position, collider);
        world.Create(
            new PositionComponent(position),
            spriteComponent,
            new InteractableComponent(50f, InteractionType.SnackMachine),
            collider
        );
    }
    
    /// <summary>
    /// Calculates the source rectangle within the tile sheet for a given Local Tile ID (LID).
    /// </summary>
    private static Rectangle GetSourceRect(int tileWidth, int tileHeight)
    {
        const int col = TileLid % TileColumns;
        const int row = TileLid / TileColumns;
        return new Rectangle(
            col * tileWidth,
            row * tileHeight,
            tileWidth,
            tileHeight
        );
    }
}