using System;
using Genesis.Architecture.Audio;

namespace Genesis.Persistence.Meta;

/// <summary>
/// Represents all global, persistent game data (stats and achievements)
/// that are saved to the fixed meta save file.
/// </summary>
[Serializable]
public class MetaData
{
    /// <summary>
    /// Global total statistics (e.g. TotalPlaytime)
    /// </summary>
    public GlobalStatsData Statistics { get; set; } = new GlobalStatsData();
    
    /// <summary>
    /// Global status of achievements
    /// </summary>
    public GlobalAchievementsData Achievements { get; set; } = new GlobalAchievementsData();

    public AudioSettings AudioSettings { get; set; } = new AudioSettings();
    public TutorialSettings TutorialSettings { get; set; } = new TutorialSettings();

    public static MetaData NewDefault()
    {
        return new MetaData()
        {
            Statistics = new GlobalStatsData(),
            Achievements = new GlobalAchievementsData(),
            AudioSettings = new AudioSettings(),
            TutorialSettings = new TutorialSettings(),
        };
    }
}