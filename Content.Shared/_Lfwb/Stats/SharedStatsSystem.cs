namespace Content.Shared._Lfwb.Stats;

public abstract class SharedStatsSystem : EntitySystem
{
    #region PublicApi

    public int GetStat(StatsComponent component, Stat stat)
    {
        return component.Stats[stat];
    }

    public void SetStatValue(EntityUid owner, StatsComponent component, Stat stat, int amount)
    {
        var newValue = Math.Clamp(amount, 0, 20);
        component.Stats[stat] = newValue;

        Dirty(owner, component);
    }

    public void ModifyStat(EntityUid owner, StatsComponent component, Stat stat, int amount)
    {
        var oldValue = component.Stats[stat];
        var newValue = Math.Max(0, oldValue + amount);

        component.Stats[stat] = newValue;

        Dirty(owner, component);
    }

    #endregion
}

