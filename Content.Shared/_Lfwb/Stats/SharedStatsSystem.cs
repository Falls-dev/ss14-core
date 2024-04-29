namespace Content.Shared._Lfwb.Stats;

public abstract class SharedStatsSystem : EntitySystem
{
    #region Data

    public static int MaxStat = 20;
    public static int MinStat = 0;

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

    #endregion
}

