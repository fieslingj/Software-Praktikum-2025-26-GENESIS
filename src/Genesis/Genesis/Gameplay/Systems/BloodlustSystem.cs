using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Simulation.Achievements;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Checks if the player has unlocked the Bloodlust achievement and triggers the unlock process
/// and unlocks a mutant-type specific achievement (e.g. Lasergazer for Mutant1).
/// </summary>
public class BloodlustSystem : IUpdateSystem
{
    private static readonly QueryDescription sTrackerQuery = new QueryDescription()
        .WithAll<BloodlustTrackerComponent, PlayerTagComponent, MutantTypeComponent>();

    private static readonly QueryDescription sRunStates = new QueryDescription()
        .WithAll<RunStatsComponent>();

    private readonly AchievementUnlocker mAchievementUnlocker;

    public BloodlustSystem(AchievementUnlocker achievementUnlocker)
    {
        mAchievementUnlocker = achievementUnlocker;
    }

    public void Update(World world, GameTime gameTime)
    {
        var isTechdemoRun = false;

        world.Query(in sRunStates, (ref RunStatsComponent runStats) =>
        {
            if (runStats.RunType == (int)RunType.Techdemo)
            {
                isTechdemoRun = true;
            }
        });

        if (isTechdemoRun)
        {
            return;
        }

        world.Query(in sTrackerQuery, (Entity player,
            ref BloodlustTrackerComponent tracker, ref MutantTypeComponent mutantType) =>
        {
            if (!tracker.IsUnlocked || tracker.HasGrantedAchievement)
            {
                return;
            }

            tracker.HasGrantedAchievement = true;
            mAchievementUnlocker.OnBloodRageUnlocked();
            mAchievementUnlocker.UnlockMutanttypeAchivement(mutantType.Type);
        });
    }
}