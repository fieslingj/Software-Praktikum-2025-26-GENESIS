using System;
using System.Collections.Generic;

namespace Genesis.Persistence.Meta;

/// <summary>
/// Represents all achievement data that persists across all gameplay runs.
/// </summary>
[Serializable]
public class GlobalAchievementsData
{
    public HashSet<string> UnlockedAchievements { get; set; } = new();

    public bool IsUnlocked(string id) => UnlockedAchievements.Contains(id);

    /// <summary>
    /// Tries to unlock the achievement with the given ID and returns true if it was newly unlocked.
    /// </summary>
    public bool TryUnlock(string id) => UnlockedAchievements.Add(id);
}