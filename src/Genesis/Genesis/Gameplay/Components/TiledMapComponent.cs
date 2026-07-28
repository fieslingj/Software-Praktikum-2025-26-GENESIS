using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;

namespace Genesis.Gameplay.Components;

public readonly struct TiledMapComponent(TiledMap map, GraphicsDevice graphicsDevice)
{
    public TiledMap Map { get; } = map;
    public TiledMapRenderer Renderer { get; } = new(graphicsDevice, map);
}