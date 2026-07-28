using System;
using System.Collections.Generic;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Purchase;
using Genesis.Gameplay.Extensions;

namespace Genesis.Persistence.Run;

/// <summary>
/// Represents the complete snapshot of the player entity's dynamic components
/// needed to resume a specific gameplay run.
/// </summary>
[Serializable]
public class SavedPlayerData()
{
    public required MutantTypeComponent MutantType { get; init; }
    public required PositionComponent Position { get; init; }
    public required HealthComponent Health { get; init; }
    public required StaminaComponent Stamina { get; init; }
    public required MassComponent Mass { get; init; }
    public required CoinsComponent Coins { get; init; }
    public required AmmoComponent Ammo { get; init; }
    public required SavedInventoryData Inventory { get; init; }
    public required BloodlustTrackerComponent BloodlustTracker { get; init; }
    public List<SavedCompanionData> Companions { get; init; } = new();

    public static SavedPlayerData Fetch(World world) => world.FetchPlayerData();
}

public static class SavedPlayerDataMethods
{
    public static SavedPlayerData FetchPlayerData(this World world)
    {
        var playerQuery = new QueryDescription().WithAll<PlayerTagComponent>();
        var player = world.GetFirstEntity(playerQuery);
        var playerData = new SavedPlayerData
        {
            Position = world.Get<PositionComponent>(player),
            Health = world.Get<HealthComponent>(player),
            MutantType = world.Get<MutantTypeComponent>(player),
            Stamina = world.Get<StaminaComponent>(player),
            Mass = world.Get<MassComponent>(player),
            Coins = world.Get<CoinsComponent>(player),
            Ammo = world.Get<AmmoComponent>(player),
            Inventory = SavedInventoryData.Fetch(world, player),
            BloodlustTracker = world.Get<BloodlustTrackerComponent>(player),
            Companions = world.FetchAllCompanions(),
        };

        return playerData;
    }
}