using Robust.Shared.Prototypes;

namespace Content.Shared._Lfwb.Stats;

[Prototype(type:"jobStatsModification")]
public sealed class JobStatsModification : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("stats")]
    public Dictionary<Stat, List<int>> StatsModification = default!;
}
