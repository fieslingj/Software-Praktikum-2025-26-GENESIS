using System;
using Genesis.Architecture;
using Genesis.Architecture.ECS; 
using Microsoft.Xna.Framework;
using Arch.Core;
using Genesis.Gameplay.Components;


namespace Genesis.Gameplay.Systems;

/// <summary>
/// This system reads the player input and translates it into a movement direction
/// (VelocityComponent) and a behavioral state (StateComponent).
/// </summary>
public class PlayerInputSystem() : IInputSystem
{
    private static readonly QueryDescription sQueryDesc = new QueryDescription()
        .WithAll<PlayerTagComponent, VelocityComponent, StateComponent>();

    public void HandleInput(World world, InputService input)
    {
        // Duck-Handling before movement/state update
        ApplyDuckState(world, input);

        // Determine the input direction based on the InputAction.
        var movementDirection = CalculateDirection(input);

        // Determine the new ActorState by movement and input.
        var newState = CalculateState(movementDirection, input);

        // Apply new input direction and state to entities with player tag and velocity component.
        world.Query(in sQueryDesc, (ref VelocityComponent velocity, ref StateComponent state) =>
        {
            if (state.Current != ActorState.Hit)
            {
                state.Previous = state.Current;
                state.Current = newState;
            }

            velocity.Direction = movementDirection;
        });
    }

    /// <summary>
    /// Handles player ducking behavior when the duck key is pressed.
    /// Reduces movement speed and hitbox height while ducking.
    /// </summary>
    private void ApplyDuckState(World world, InputService input)
    {
        var isDuckDown = input.IsActionDown(InputAction.Duck);

        world.Query(in sQueryDesc, (Entity entity, ref VelocityComponent velocity, ref StateComponent state) =>
        {
            // Skip if entity doesn't have duck behavior component
            if (!world.Has<DuckBehaviorComponent>(entity)) { return; }
            
            ref var duck = ref world.Get<DuckBehaviorComponent>(entity);
            
            if (isDuckDown)
            {
                // Start ducking if not already ducking
                if (duck.ActionTimer <= 0)
                {
                    // Store original hitbox values for restoration later
                    if (world.Has<HitBoxComponent>(entity))
                    {
                        var hitbox = world.Get<HitBoxComponent>(entity);
                        duck.OriginalHitboxSize = hitbox.Size;
                        duck.OriginalHitboxOffset = hitbox.Offset;
                    }
                }
                
                // Keep ducking active (timer checked by AnimationSystem)
                duck.ActionTimer = 1f;
                
                // Reduce movement speed to 50% while ducking
                velocity.Value = velocity.BaseSpeed * 0.5f;
                
                // Reduce hitbox height to 60% (makes player harder to hit)
                if (world.Has<HitBoxComponent>(entity))
                {
                    ref var hitbox = ref world.Get<HitBoxComponent>(entity);
                    hitbox.Size = new Vector2(hitbox.Size.X, duck.OriginalHitboxSize.Y * 0.6f);
                }
            }
            else
            {
                // Stop ducking when key is released
                if (duck.ActionTimer > 0)
                {
                    duck.ActionTimer = 0f;
                    
                    // Restore original movement speed
                    velocity.Value = velocity.BaseSpeed;
                    
                    // Restore original hitbox size and offset
                    if (world.Has<HitBoxComponent>(entity))
                    {
                        ref var hitbox = ref world.Get<HitBoxComponent>(entity);
                        hitbox.Size = duck.OriginalHitboxSize;
                        hitbox.Offset = duck.OriginalHitboxOffset;
                    }
                }
            }
        });
    }

    /// <summary>
    /// Determine the Movement Direction.
    /// </summary>
    /// <returns>Normalized Direction Vector.</returns>
    private static Vector2 CalculateDirection(InputService input)
    {
        var inputDir = Vector2.Zero;
        if (input.IsActionDown(InputAction.MoveUp))    {inputDir.Y -= 1;}
        if (input.IsActionDown(InputAction.MoveDown))  {inputDir.Y += 1;}
        if (input.IsActionDown(InputAction.MoveLeft))  {inputDir.X -= 1;}
        if (input.IsActionDown(InputAction.MoveRight)) {inputDir.X += 1;}
        
        if (inputDir != Vector2.Zero) {inputDir.Normalize();}
        
        return inputDir;
    }

    /// <summary>
    /// Determine whether the Actor is Idle, Walking or Sprinting.
    /// </summary>
    /// <returns>The determined state.</returns>
    private static ActorState CalculateState(Vector2 direction, InputService inputService)
    {
        if (direction == Vector2.Zero) {return ActorState.Idle;}
        return inputService.IsActionDown(InputAction.Sprint) ? ActorState.Sprinting : ActorState.Walking;
    }
}