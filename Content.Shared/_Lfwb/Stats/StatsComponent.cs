using Robust.Shared.GameStates;

namespace Content.Shared._Lfwb.Stats;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StatsComponent : Component
{
    [DataField("statsPreset")]
    [ValidatePrototypeId<StatsPresetPrototype>]
    public string StatsPreset = "HumanStatsPreset";

    [DataField("stats"), AutoNetworkedField]
    public Dictionary<Stat, int> Stats = new()
    {
        {Stat.Strength, 10},
        {Stat.Intelligence, 10},
        {Stat.Dexterity, 10},
        {Stat.Endurance, 10},
        {Stat.Luck, 2}
    };
}
