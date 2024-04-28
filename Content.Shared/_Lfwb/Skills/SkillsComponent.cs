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

    #region Threshold Operations

    public void ApplyThreshold(Skill skill, FixedPoint2 value)
    {
        var newValue = Skills[skill].Item2 + value;
        newValue = FixedPoint2.Clamp(newValue, 0, 100);

        if (newValue >= 100)
        {
            ImproveSkill(skill);
            return;
        }

        Skills[skill] = (Skills[skill].Item1, newValue);
    }

    #endregion

    #region Skill Operations

    public SkillLevel GetSkillLevel(Skill skill)
    {
        return Skills[skill].Item1;
    }

    public void SetSkillLevel(Skill skill, SkillLevel level)
    {
        Skills[skill] = (level, 0);
    }

    public void ImproveSkill(Skill skill)
    {
        Skills[skill] = (NextSkillLevel(Skills[skill].Item1), 0);
    }

    public void DecreaseSkill(Skill skill)
    {
        Skills[skill] = (PreviousSkillLevel(Skills[skill].Item1), 0);
    }

    private SkillLevel NextSkillLevel(SkillLevel currentLevel)
    {
        return currentLevel switch
        {
            SkillLevel.Weak => SkillLevel.Average,
            SkillLevel.Average => SkillLevel.Skilled,
            SkillLevel.Skilled => SkillLevel.Master,
            SkillLevel.Master => SkillLevel.Legendary,
            SkillLevel.Legendary => SkillLevel.Legendary,
            _ => currentLevel
        };
    }

    private SkillLevel PreviousSkillLevel(SkillLevel currentLevel)
    {
        return currentLevel switch
        {
            SkillLevel.Weak => SkillLevel.Weak,
            SkillLevel.Average => SkillLevel.Weak,
            SkillLevel.Skilled => SkillLevel.Average,
            SkillLevel.Master => SkillLevel.Skilled,
            SkillLevel.Legendary => SkillLevel.Master,
            _ => currentLevel
        };
    }

    #endregion
}
