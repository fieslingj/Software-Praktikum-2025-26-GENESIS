using System;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;

namespace Genesis.Persistence.Run;

/// <summary>
/// Data required to save and load a run
/// </summary>
[Serializable]
public class RunSaveData
{
    private const string CurrentVersion = "1.0.0";
    
    // --- Metadata for the Save Slot ---
    public required string Version { get; set; }
    public required DateTime Date { get; set; }
    public required int Seed { get; set; }
    public required RunSessionComponent Session { get; set; }
    public required RunStatsComponent RunStats { get; set; }
    public required RunTimerComponent Timer { get; set; }
    
    // --- Level State ---
    public required FloorLayoutComponent Floor { get; set; }
    public required BossQueueComponent BossQueue { get; set; }
    
    // --- Player State (Components) ---
    /// <summary>
    /// Snapshot of the player entity's core dynamic components
    /// </summary>
    public required SavedPlayerData PlayerData { get; set; }

    public static RunSaveData Fetch(World world)
    {
        return new RunSaveData()
        {
            Version = CurrentVersion,
            Date = DateTime.UtcNow,
            Seed = world.GetResource<RandomService>().Seed,
            PlayerData = world.FetchPlayerData(),
            Floor = world.GetResource<FloorLayoutComponent>(),
            BossQueue = world.GetResource<BossQueueComponent>(),
            Session = world.GetResource<RunSessionComponent>(),
            RunStats = world.GetResource<RunStatsComponent>(),
            Timer = world.GetResource<RunTimerComponent>(),
        };
    }
}