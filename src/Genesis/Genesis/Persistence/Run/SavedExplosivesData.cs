using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Components;

namespace Genesis.Persistence.Run;

public readonly struct SavedExplosivesData(PositionComponent position)
{
    public PositionComponent Position { get; } = position;
}

public static class SavedExplosivesDataMethods
{
    private static readonly QueryDescription sQuery =
        new QueryDescription().WithAll<RemoteExplosiveComponent, PositionComponent>();
    
    public static List<SavedExplosivesData> FetchAllExplosives(this World world)
    {
        var savedData = new List<SavedExplosivesData>();
        world.Query(sQuery, (ref PositionComponent pos) => savedData.Add(new SavedExplosivesData(pos)));
        return savedData;
    }
}