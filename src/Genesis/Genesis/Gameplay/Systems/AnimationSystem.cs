using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class AnimationSystem : IUpdateSystem
{
    private static readonly QueryDescription sQueryDesc = new QueryDescription()
        .WithAll<SpriteComponent, SimpleAnimationComponent>();

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in sQueryDesc, (Entity entity, ref SpriteComponent sprite, ref SimpleAnimationComponent anim) =>
        {
            // Skip processing if the entity has an inactive EffectComponent
            if (world.Has<EffectComponent>(entity) && !world.Get<EffectComponent>(entity).Active)
            {
                return;
            }

            if (anim.IsFinished) {return;}

            // Update frame timer
            anim.FrameTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (anim.FrameTimer >= anim.FrameDuration)
            {
                anim.FrameTimer -= anim.FrameDuration;
                anim.CurrentFrame++;
            }

            int currentColumn = 0;
            int currentRow = 0;

            // --- Determine Entity Type (Character vs. Object) ---

            // Case A: Character (Has StateComponent)
            // Characters use different rows for different states (Idle, Walk, Attack)
            if (world.Has<StateComponent>(entity))
            {
                var state = world.Get<StateComponent>(entity);
                int maxFrames = 1;
                
                // Check if entity is ducking
                bool isDucking = world.Has<DuckBehaviorComponent>(entity) && 
                                 world.Get<DuckBehaviorComponent>(entity).ActionTimer > 0;
    
                // Check if entity is moving
                bool isMoving = world.Has<VelocityComponent>(entity) && 
                                world.Get<VelocityComponent>(entity).Direction != Vector2.Zero;

                // If ducking, use rows 2 (idle) or 3 (walking)
                if (isDucking)
                {
                    if (isMoving)
                    {
                        currentRow = 3; // Row 3: Ducking + Walking
                        maxFrames = anim.FramesInWalk;
                    }
                    else
                    {
                        currentRow = 2; // Row 2: Ducking + Idle
                        maxFrames = anim.FramesInIdle;
                    }
                }
                else
                {
                    switch (state.Current)
                    {
                        case ActorState.Idle:
                        case ActorState.Attacking: // fix because attack animation is not made yet
                            currentRow = 0; // Row 0
                            maxFrames = anim.FramesInIdle;
                            break;
                        case ActorState.Walking:
                        case ActorState.Sprinting:
                            currentRow = 1; // Row 1
                            maxFrames = anim.FramesInWalk;
                            break;
                        // case ActorState.Attacking:
                        // currentRow = 2; 
                        // maxFrames = anim.FramesInIdle; 
                        // break;
                    }
                }

                // Handle looping for characters
                     if (anim.CurrentFrame >= maxFrames)
                     {
                         anim.CurrentFrame = 0;
                     }
                     
                     // Characters maps frames directly to columns (no multi-row grid wrapping for a single state)
                     currentColumn = anim.CurrentFrame;}
            
            // Case B: Object (Doors, Traps, Effects)
            // Objects use linear frame counting that may wrap across rows based on FramesPerRow
            else 
            {
                // Handle looping or finishing behavior
                if (anim.CurrentFrame >= anim.FrameCount)
                {
                    if (anim.IsLooping)
                    {
                        anim.CurrentFrame = 0;
                    }
                    else
                    {
                        anim.CurrentFrame = anim.FrameCount - 1;
                        anim.IsFinished = true;
                    }
                }

                // Calculate grid position
                currentColumn = anim.CurrentFrame % anim.FramesPerRow;
                currentRow = anim.CurrentFrame / anim.FramesPerRow;
            }

            // --- Update Sprite Source Rectangle ---
            var newSource = new Rectangle(
                x: currentColumn * anim.FrameWidth + anim.FrameWidthOffset * (currentColumn + 1),
                y: currentRow * anim.FrameHeight + anim.FrameHeightOffset * (currentRow + 1),
                width: anim.FrameWidth,
                height: anim.FrameHeight
            );
            
            sprite.SourceRect = newSource;
        });
    }
}