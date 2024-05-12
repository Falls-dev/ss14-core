using Robust.Shared.Prototypes;

namespace Content.Shared._Lfwb.Skills;

[Prototype(type:"jobSkillsModification")]
public sealed class JobSkillsModification : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("skills")]
    public Dictionary<Skill, SkillLevel> StatsModification = default!;
}
