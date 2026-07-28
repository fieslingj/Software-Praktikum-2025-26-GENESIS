using System;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public class ElevatorEntity
{
    /// <summary>
    /// Creates a door entity from a Tiled map object.
    /// </summary>
    public static Entity Create(World world, TiledMapObject mapObject, Texture2D elevatorTexture, bool isOpen)
    {
        
        var size = new Vector2(mapObject.Size.Width, mapObject.Size.Height);
        var position = new Vector2(
            mapObject.Position.X + size.X / 2f,
            mapObject.Position.Y + size.Y / 2f
        );
        Rectangle sourceRect;
        if (isOpen)
        {
            //um nicht überspringen zu können
            var collideropen = new ColliderComponent(new Vector2(size.X,size.Y));
            world.Create(new PositionComponent(position), collideropen);
            
            var gridMap0 = world.GetResource<GridMap>();
            if (gridMap0 == null)
            {
                Console.WriteLine("[Elevator Entity] Warning: GridMap resource is missing!");
            }
            gridMap0?.MarkColliderAsUnwalkable(position, collideropen);
            
            sourceRect = new Rectangle(352, 0, 32, 32);
            return world.Create(new SpriteComponent(elevatorTexture, sourceRect),new PositionComponent(position));
            
        }
        
        sourceRect = new Rectangle(0, 0, 32, 32);
        var collider = new ColliderComponent(new Vector2(size.X,8),new Vector2(0,size.Y / 2));
        var gridMap = world.GetResource<GridMap>();
        if (gridMap == null)
        {
            Console.WriteLine("[Elevator Entity] Warning: GridMap resource is missing!");
        }
        gridMap?.MarkColliderAsUnwalkable(position, collider);

        return world.Create(new SpriteComponent(elevatorTexture, sourceRect), new PositionComponent(position),
            new InteractableComponent(50f, InteractionType.Elevator), collider);
    }
}