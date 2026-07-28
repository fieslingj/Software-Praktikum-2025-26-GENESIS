using System;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public static class DoorEntity
{
    private const int FrameSize = 32;
    
    /// <summary>
    /// Creates a door entity from a Tiled map object.
    /// </summary>
    public static Entity Create(World world, TiledMapObject mapObject, Texture2D doorTexture, bool isOpen)
    {
        var offset = new Vector2(mapObject.Size.Width / 2, mapObject.Size.Height - 4);
        var position = mapObject.Position + offset;

        if (!mapObject.Properties.TryGetValue("DoorDirection", out var dirStr)) { throw new ArgumentException(); }
        var direction = dirStr switch
        {
            "North" => DoorDirection.North,
            "East" => DoorDirection.East,
            "South" => DoorDirection.South,
            "West" => DoorDirection.West,
            _ => throw new ArgumentException()
        };
        
        var entity = world.Create(
            new PositionComponent(position),
            GetSpriteComponent(doorTexture, isOpen, offset),
            CreateAnimation(),
            new DoorComponent(direction, isOpen),
            new InteractableComponent(50f, InteractionType.Door)
        );
        
        if (!isOpen) 
        { 
            var collider = new ColliderComponent(new Vector2(mapObject.Size.Width, 8));
            world.Add(entity, collider); 
            var gridMap = world.GetResource<GridMap>();
            if (gridMap == null)
            {
                Console.WriteLine("[DoorEntity] Warning: GridMap resource is missing!");
            }
            gridMap?.MarkColliderAsUnwalkable(position, collider);
        }

        
        return entity;
    }

    private static SpriteComponent GetSpriteComponent(Texture2D texture, bool isOpen, Vector2 offset)
    {
        const int startY = 0;
        const int startXClosed = 0;
        const int startXOpen = 11 * FrameSize;

        var startX = isOpen ? startXOpen : startXClosed;
        var sourceRect = new Rectangle(startX, startY, FrameSize, FrameSize);

        return new SpriteComponent(
            texture,
            sourceRect,
            layerDepth: 0.1f,
            scale: 1.0f
        ){ Origin = offset };
    }

    private static SimpleAnimationComponent CreateAnimation()
    {
        var animationComponent = new SimpleAnimationComponent(
            frameWidth: FrameSize,
            frameHeight: FrameSize,
            frameCount: 12,
            framesPerRow: 12,
            frameDuration: 100f, // milliseconds per frame
            isLooping: false
        );
        animationComponent.IsFinished = true;

        return animationComponent;
    }
}