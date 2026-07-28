using System;
using System.Diagnostics;
using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;

namespace Genesis.Persistence.Run;

/// <summary>
/// identifies from where the statistics saving was triggered
/// </summary>
public enum StatisticCallingState
{
    /// <summary>Generic save (Pause or Room switch)</summary>
    Generic = 0,

    /// <summary>WinState</summary>
    Win = 1,

    /// <summary>GameOverState</summary>
    GameOver = 2,
}

/// <summary>
/// Saves the relevant statistic data at a certain point in time.
/// </summary>
[Serializable]
public class SavedStatisticData
{
    public required RunSessionComponent Session { get; init; }
    public required RunStatsComponent Stats { get; init; }
    public required RunTimerComponent RunTimer { get; init; }

    /// Identifies from where the statistics saving was triggered
    public required StatisticCallingState CallingState { get; init; }

    public static SavedStatisticData Fetch(World world, StatisticCallingState state, GameServices service)
        => world.FetchStatisticData(state, service);
}
public static class SavedStatisticDataMethods
{
    public static SavedStatisticData FetchStatisticData(this World world, StatisticCallingState state, GameServices services)
    {
        var savedRunStats = world.GetResource<RunStatsComponent>();
        var savedSession = world.GetResource<RunSessionComponent>();
        var savedRunTimer = world.GetResource<RunTimerComponent>();
        var metaData = SaveManager.LoadMeta();
        var stats = services.MetaData.Statistics;

        if (savedRunStats.RunType == (int)RunType.Techdemo)
        {
            Debug.WriteLine("[StatisticData] Techdemo run detected, skipping statistic saving.");
            return new SavedStatisticData
            {
                Session = savedSession,
                Stats = savedRunStats,
                RunTimer = savedRunTimer,
                CallingState = state
            };
        }

        // Save relevant statistics based on the calling state
        switch (state)
        {
            case StatisticCallingState.Win:
                Debug.WriteLine("[Win] Statistics are getting saved");
                stats.TotalSuccessfulRuns++;
                stats.TotalPlaytimeSeconds += (float)savedRunTimer.TotalSeconds;
                stats.TotalEnemiesDefeated += savedRunStats.EnemiesDefeated;
                stats.TotalDamageDealt += savedRunStats.DamageDealt;
                stats.TotalDamageTaken += savedRunStats.DamageTaken;
                stats.TotalRoomsExplored += savedRunStats.RoomsExplored;

                if (stats.FastestRunSeconds <= 0 || savedRunTimer.TotalSeconds < stats.FastestRunSeconds)
                {
                    stats.FastestRunSeconds = (float)savedRunTimer.TotalSeconds;
                }
                break;

            case StatisticCallingState.GameOver:
                Debug.WriteLine("[GameOver] Statistics are getting saved");
                stats.TotalDeaths++;
                stats.TotalPlaytimeSeconds += (float)savedRunTimer.TotalSeconds;
                stats.TotalEnemiesDefeated += savedRunStats.EnemiesDefeated;
                stats.TotalDamageDealt += savedRunStats.DamageDealt;
                stats.TotalDamageTaken += savedRunStats.DamageTaken;
                stats.TotalRoomsExplored += savedRunStats.RoomsExplored;
                break;

            case StatisticCallingState.Generic:
            default:

                stats.TotalPlaytimeSeconds += (float)savedRunTimer.TotalSeconds - stats.LastSavedPlaytimeSeconds;
                stats.LastSavedPlaytimeSeconds = (float)savedRunTimer.TotalSeconds;
                stats.TotalDamageDealt += savedRunStats.DamageDealt;
                stats.TotalDamageTaken += savedRunStats.DamageTaken;
                stats.TotalEnemiesDefeated += savedRunStats.EnemiesDefeated;
                stats.TotalRoomsExplored += savedRunStats.RoomsExplored;

                // reset to avoid double counting on next save
                savedRunStats.DamageDealt = 0;
                savedRunStats.DamageTaken = 0;
                savedRunStats.EnemiesDefeated = 0;
                savedRunStats.RoomsExplored = 0;
                world.SetResource(savedRunStats);
                break;
        }

        // Set updated stats back to meta data insert to new world and save
        metaData.Statistics = stats;
        services.MetaData.Statistics = stats;
        // Update the world's MetaDataComponent so systems reading MetaDataComponent see the updated values
        world.SetResource(new MetaDataComponent(metaData));
        SaveManager.SaveMeta(metaData);

        return new SavedStatisticData
        {
            Session = savedSession,
            Stats = savedRunStats,
            RunTimer = savedRunTimer,
            CallingState = state
        };
    }
}