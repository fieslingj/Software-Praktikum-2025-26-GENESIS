using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;

namespace Genesis.Persistence.Run;

[Serializable]
public struct SavedCorpseData
{
    public required CorpseComponent Type { get; init; }
    public required PositionComponent Position { get; init; }
    public required AmmoComponent Ammo { get; init; }
}

public static class SavedCorpseDataMethods
{
    private static readonly QueryDescription sQuery = new QueryDescription().WithAll<CorpseComponent>();
    
    public static List<SavedCorpseData> FetchAllCorpses(this World world)
    {
        var corpses = new List<SavedCorpseData>();
        world.Query(sQuery, entity => corpses.Add(FetchCorpseData(world, entity)));
        return corpses;
    }

    private static SavedCorpseData FetchCorpseData(World world, Entity corpse)
    {
        return new SavedCorpseData()
        {
            Type = world.Get<CorpseComponent>(corpse),
            Position = world.Get<PositionComponent>(corpse),
            Ammo = world.Get<AmmoComponent>(corpse),
        };
    }
}