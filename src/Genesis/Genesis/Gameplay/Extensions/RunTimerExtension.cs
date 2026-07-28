using System;
using Arch.Core;
using Genesis.Gameplay.Components;

namespace Genesis.Gameplay.Extensions;

public static class RunTimerExtensions
{
    public static TimeSpan GetCurrentRunDuration(this World world)
    {
        var query = new QueryDescription().WithAll<RunTimerComponent>();
        var entity = world.GetFirstEntity(query);

        if (entity == Entity.Null) { return TimeSpan.Zero; }

        ref var timer = ref world.Get<RunTimerComponent>(entity);
        return TimeSpan.FromSeconds(timer.TotalSeconds);
    }

    public static double GetCurrentRunTimeSeconds(this World world)
    {
        var query = new QueryDescription().WithAll<RunTimerComponent>();
        var entity = world.GetFirstEntity(query);

        return world.Get<RunTimerComponent>(entity).TotalSeconds;
    }
}