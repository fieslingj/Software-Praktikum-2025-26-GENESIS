using System;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

public class ScreenShakingSystem(CameraService camera, RandomService rng) : IUpdateSystem
{
    private static readonly QueryDescription sShakeQuery = new QueryDescription()
        .WithAll<ShakeSourceComponent>();
    
    public void Update(World world, GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // 1. Find the shake source with the maximum trauma
        var maxTrauma = 0f;
        ShakeSourceComponent strongestShake = default;
        var foundAny = false;

        world.Query(in sShakeQuery, (Entity entity, ref ShakeSourceComponent shake) =>
        {
            if (shake.Trauma > maxTrauma)
            {
                maxTrauma = shake.Trauma;
                strongestShake = shake;
                foundAny = true;
            }

            // Decay trauma
            if (!shake.IsContinuous)
            {
                shake.Trauma -= shake.Decay * dt;
            }

            // Remove if trauma depleted
            if (shake.Trauma <= 0f)
            {
                world.Destroy(entity);
            }
        });

        // 2. Apply shake to camera if any active source exists
        if (foundAny && maxTrauma > 0f)
        {
            // Trauma is usually clamped between 0 and 1 for calculation
            var trauma = MathHelper.Clamp(maxTrauma, 0f, 1f);
            
            // Shake intensity is often trauma squared or cubed for better feel
            var shakeFactor = trauma * trauma;

            // Calculate random offset and rotation
            // Using RandomService to get random values in range [-1, 1]
            var offsetX = (rng.NextFloat() * 2f - 1f) * strongestShake.MaxOffset * shakeFactor;
            var offsetY = (rng.NextFloat() * 2f - 1f) * strongestShake.MaxOffset * shakeFactor;
            var rotation = (rng.NextFloat() * 2f - 1f) * strongestShake.MaxRotation * shakeFactor;

            camera.ShakeOffset = new Vector2(offsetX, offsetY);
            camera.ShakeRotation = rotation;
        }
        else
        {
            // Reset if no shake
            camera.ShakeOffset = Vector2.Zero;
            camera.ShakeRotation = 0f;
        }
    }
}