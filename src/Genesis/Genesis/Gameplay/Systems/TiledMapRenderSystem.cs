using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled.Renderers; // Wichtig für .OfType<T>()

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Dieses System findet die Map-Entität und zeichnet ALLE
/// Kachel-Ebenen (TmxLayer) nacheinander.
/// </summary>
public class TiledMapRenderSystem(CameraService camera) : IUpdateSystem, IDrawSystem
{
    private static readonly QueryDescription sRendererQuery = new QueryDescription()
        .WithAll<TiledMapComponent>();
    
    public void Update(World world, GameTime gameTime)
    {
        var renderer = GetRenderer(world);
        renderer?.Update(gameTime);
    }

    public void Draw(World world, SpriteBatch spriteBatch, bool ySorting=false)
    {
        var renderer = GetRenderer(world);
        renderer?.Draw(viewMatrix: camera.GetViewMatrix(), depth: 0f);
    }

    private static TiledMapRenderer GetRenderer(World world)
    {
        var entity = world.GetFirstEntity(sRendererQuery);
        return entity == Entity.Null ? null : world.Get<TiledMapComponent>(entity).Renderer;
    }
}