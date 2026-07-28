using Genesis.Architecture.ECS;
using Microsoft.Xna.Framework;
using Arch.Core;
using Genesis.Gameplay.Components;
using System;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Manages stamina drain and regeneration for entities with a StaminaComponent,
/// and removes the SprintingTagComponent when stamina is depleted.
/// </summary>

public class StaminaSystem : IUpdateSystem
{
    private const float SDrainRate = 20f;
    private const float SRegenRate = 15f;
    private static readonly QueryDescription sStaminaQuery = new QueryDescription()
        .WithAll<StaminaComponent, StateComponent>();
    
    public void Update(World world, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        world.Query(in sStaminaQuery, 
            (Entity entity, ref StaminaComponent stamina, ref StateComponent state) =>
        {
            if (state.Current == ActorState.Sprinting)
            {
                // Drain the stamina while the entity is sprinting.
                stamina.Current -= deltaTime * SDrainRate;
                if (stamina.Current <= 0f)
                {
                    stamina.Current = 0f;
                    state.Current = ActorState.Walking;
                }
            }
            else
            {
                // Regenerate the stamina while the entity is not sprinting.
                stamina.Current = Math.Min(stamina.Current + deltaTime * SRegenRate, stamina.Max);
            }
        });
    }
}