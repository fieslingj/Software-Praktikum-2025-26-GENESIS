using Arch.Core;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using Genesis.Architecture.ECS;

namespace Genesis.Gameplay.Systems;

public class RunTimerSystem : IUpdateSystem
{
    private readonly QueryDescription mTimerQuery = new QueryDescription().WithAll<RunTimerComponent>();

    public void Update(World world, GameTime gameTime)
    {
        world.Query(in mTimerQuery, (ref RunTimerComponent timer) =>
        {
            // Accumulate the total elapsed time since the last frame.
            // This stops automatically when the gameplay system group is not updated (e.g. during PAUSE).
            timer.TotalSeconds += gameTime.ElapsedGameTime.TotalSeconds;
        });
    }
}