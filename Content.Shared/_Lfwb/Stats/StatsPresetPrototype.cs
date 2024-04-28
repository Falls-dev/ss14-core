using Robust.Shared.Prototypes;

namespace Content.Shared._Lfwb.Stats;

[Prototype("statsPreset")]
public class StatsPresetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("preset")]
    public Dictionary<Stat, List<int>> Preset = default!;
}
