using System;
using Genesis.Architecture;
using Genesis.Architecture.Persistence;
using Genesis.Gameplay.Components;
using Genesis.Persistence.Meta;

namespace Genesis.Simulation.Achievements;

public class AchievementUnlocker
{
    private readonly MetaData mMeta;
    // TODO: Adjust time limit after playtesting.
    public const float SpeedrunTimeLimitMinutes = 3;

    public AchievementUnlocker(GameServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(services.MetaData);

        mMeta = services.MetaData;
    }

    private GlobalAchievementsData Achievements => mMeta.Achievements;

    public void OnBloodRageUnlocked()
    {
        Achievements.TryUnlock(AchievementIds.BloodRage);
        SaveManager.SaveMeta(mMeta);
    }

    public void UnlockMutanttypeAchivement(MutantType mutantType)
    {
        if (!Achievements.IsUnlocked(AchievementIds.BloodRage)) { return; }

        var unlocked = false;

        switch (mutantType)
        {
            case MutantType.Mutant1:
                unlocked = Achievements.TryUnlock(AchievementIds.LaserGazer);
                break;
            case MutantType.Mutant2:
                unlocked = Achievements.TryUnlock(AchievementIds.GoingViral);
                break;
            case MutantType.Mutant3:
                unlocked = Achievements.TryUnlock(AchievementIds.HeavyMetal);
                break;
        }

        if (unlocked)
        {
            SaveManager.SaveMeta(mMeta);
        }
    }

    public void UnlockSpeedRunnerAchivement(TimeSpan runDuration)
    {
        if (runDuration.TotalMinutes <= SpeedrunTimeLimitMinutes && Achievements.TryUnlock(AchievementIds.Speedrunner))
        {
                SaveManager.SaveMeta(mMeta);
        }
    }
}