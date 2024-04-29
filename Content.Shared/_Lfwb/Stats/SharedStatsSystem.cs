using Content.Shared._Lfwb.PredictedRandom;

namespace Content.Shared._Lfwb.Stats;

public abstract class SharedStatsSystem : EntitySystem
{
    [Dependency] private readonly PredictedRandomSystem _predictedRandomSystem = default!;

    #region Data

    public int MaxStat = 20;
    public int MinStat = 0;

    #endregion

    #region PublicApi

    public int GetStat(EntityUid owner, Stat stat)
    {
        return !TryComp<StatsComponent>(owner, out var statsComponent)
            ? 0
            : statsComponent.Stats[stat];
    }

    public void SetStatValue(EntityUid owner, Stat stat, int amount)
    {
        if (!TryComp<StatsComponent>(owner, out var statsComponent))
            return;

        var newValue = Math.Clamp(amount, MinStat, MaxStat);
        statsComponent.Stats[stat] = newValue;

        Dirty(owner, statsComponent);
    }

    public void ModifyStat(EntityUid owner, Stat stat, int amount)
    {
        if (!TryComp<StatsComponent>(owner, out var statsComponent))
            return;

        var oldValue = statsComponent.Stats[stat];
        var newValue = oldValue + amount;

        newValue = Math.Clamp(newValue, MinStat, MaxStat);

        statsComponent.Stats[stat] = newValue;

        Dirty(owner, statsComponent);
    }

    public (int, string, bool) D20(int stat)
    {
        var roll = _predictedRandomSystem.Next(1, 21);
        return roll switch
        {
            1 => (roll, "Критическая неудача!", false),
            20 => (roll, "Критическая удача!", true),
            _ => roll <= stat
                ? (roll, "Удача!", true)
                : (roll, "Неудача!", false)
        };
    }

    public (int, string, bool) D20(int stat, int luck)
    {
        var roll = _predictedRandomSystem.Next(1, 21);
        return roll switch
        {
            1 => (roll, "Критическая неудача!", false),
            20 => (roll, "Критическая удача!", true),
            _ => roll <= stat + luck
                ? (roll, "Удача!", true)
                : (roll, "Неудача!", false)
        };
    }

    #endregion
}

