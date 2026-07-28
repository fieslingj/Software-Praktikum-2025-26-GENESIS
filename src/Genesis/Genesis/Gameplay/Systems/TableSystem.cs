using System.Diagnostics;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace Genesis.Gameplay.Systems;

public class TableSystem(ContentManager content, AudioService audio) : IUpdateSystem
{
    private static readonly QueryDescription sTableQuery = new QueryDescription()
        .WithAll<TableComponent, PositionComponent, SpriteComponent>();

    private Texture2D TableFlippedTexture { get; } = content.Load<Texture2D>("Sprites/Props/TableFlipped");
    private const string TableFlipSound = "Sounds/Effects/TableFlip";

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sTableQuery,
            (Entity entity,
                ref TableComponent table,
                ref PositionComponent position,
                ref SpriteComponent sprite) =>
            {

                if (table.State == TableState.Flipped && !table.IsInteractedWith)
                {
                    sprite.SpriteSheet = TableFlippedTexture;
                    table.IsInteractedWith = true;
                    audio.PlaySfx(TableFlipSound);
                }
            });
    }
}
