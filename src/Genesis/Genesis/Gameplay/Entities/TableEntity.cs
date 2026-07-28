using System;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public static class TableEntity
{
    // Change interaction radius as needed
    private const float InteractionRadius = 64f;

    /// <summary>
    /// Creates a table entity in the given world at the specified position
    /// <summary/>

    public static Entity Create(World world, TiledMapObject table, Texture2D spriteSheet)
    {
        var gridMap = world.GetResource<GridMap>();
        if (gridMap == null)
        {
            Console.WriteLine("[TableEntity] Warning: GridMap resource is missing!");
        }
        
        var sourceRect = new Rectangle(0, 0, spriteSheet.Width, spriteSheet.Height);

        var spriteComponent = new SpriteComponent(
            spriteSheet,
            sourceRect,
            0.1f,
            scale: 1.0f
        );


        var position = table.Position + new Vector2(table.Size.Width / 2f, table.Size.Height / 2f);
        var colliderOffsetX = 2f;
        var colliderOffsetY = 0f;

        var colliderSize = new Vector2(sourceRect.Width + colliderOffsetX, sourceRect.Height - colliderOffsetY);
        var colliderComponent = new ColliderComponent(colliderSize);
        gridMap.MarkColliderAsUnwalkable(position, colliderComponent);

        var interactableComponent = new InteractableComponent(
            InteractionRadius,
            InteractionType.Table
        );

        var entity = world.Create(
            new PositionComponent(position),
            spriteComponent,
            new TableComponent(TableState.Standing),
            colliderComponent,
            interactableComponent
            );
        return entity;
    }
}
