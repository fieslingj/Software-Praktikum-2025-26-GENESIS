using Arch.Core;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Entities;

public static class MapEntity
{
    public static void Create(World world, Texture2D texture, Vector2 position)
    {
        var sourceRect = texture.Bounds;
                const float layerDepth = 0.1f;
        world.Create(
            new SpriteComponent(texture, sourceRect, layerDepth),
            new PositionComponent(position)
        );
    }
}