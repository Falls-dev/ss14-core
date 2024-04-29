using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Lfwb.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SkillsComponent : Component
{
    [DataField("skills"), AutoNetworkedField]
    public Dictionary<Skill, (SkillLevel, FixedPoint2)> Skills = new()
    {
        { Skill.Melee, (SkillLevel.Weak, 0)},
        { Skill.Ranged,(SkillLevel.Weak, 0)},
        { Skill.Medicine, (SkillLevel.Weak, 0)},
        { Skill.Engineering, (SkillLevel.Weak, 0)},
    };
}
