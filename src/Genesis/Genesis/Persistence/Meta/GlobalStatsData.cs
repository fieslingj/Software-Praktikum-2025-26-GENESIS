using System;

namespace Genesis.Persistence.Meta;

[Serializable]
public struct GlobalStatsData
{
    public float TotalPlaytimeSeconds { get; set; }
    public float LastSavedPlaytimeSeconds { get; set; }
    public int TotalEnemiesDefeated { get; set; }
    public float TotalDamageDealt { get; set; }
    public float TotalDamageTaken { get; set; }
    public int TotalSuccessfulRuns { get; set; }
    public int TotalDeaths { get; set; }
    public float FastestRunSeconds { get; set; }
    public int TotalRoomsExplored { get; set; }
    public double CountedRunSeconds { get; set; }
}