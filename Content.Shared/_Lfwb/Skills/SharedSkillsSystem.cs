using Content.Shared.FixedPoint;
using Content.Shared.Popups;

namespace Content.Shared._Lfwb.Skills;

public class SharedSkillsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    #region Data

    public readonly Dictionary<SkillLevel, float> SkillLevelToDelay = new()
    {
        { SkillLevel.Weak , 8f},
        { SkillLevel.Average , 6f},
        { SkillLevel.Skilled , 3f},
        { SkillLevel.Master , 2f},
        { SkillLevel.Legendary , 1f},
    };

    public readonly Dictionary<SkillLevel, float> SkillLevelToChance = new()
    {
        { SkillLevel.Weak , 0.2f},
        { SkillLevel.Average , 0.4f},
        { SkillLevel.Skilled , 0.6f},
        { SkillLevel.Master , 0.8f},
        { SkillLevel.Legendary , 1f},
    };

    public readonly Dictionary<SkillLevel, int> SkillLevelToAddition = new()
    {
        { SkillLevel.Weak , 0},
        { SkillLevel.Average , 2},
        { SkillLevel.Skilled , 4},
        { SkillLevel.Master , 6},
        { SkillLevel.Legendary , 8},
    };

    public readonly Dictionary<SkillLevel, int> SkillLevelToSkibidi = new()
    {
        { SkillLevel.Weak , -3},
        { SkillLevel.Average , -2},
        { SkillLevel.Skilled , 1},
        { SkillLevel.Master , 2},
        { SkillLevel.Legendary , 3},
    };

    #endregion

    #region PublicApi

    public void ApplySkillThreshold(EntityUid owner, Skill skill, FixedPoint2 value)
    {
        if (!TryComp<SkillsComponent>(owner, out var skillsComponent))
            return;

        var newValue = skillsComponent.Skills[skill].Item2 + value;
        newValue = FixedPoint2.Clamp(newValue, 0, 100);

        if (newValue >= 100)
        {
            ImproveSkill(owner, skill);
            return;
        }

        skillsComponent.Skills[skill] = (skillsComponent.Skills[skill].Item1, newValue);

        Dirty(owner, skillsComponent);
    }

    public SkillLevel GetSkillLevel(EntityUid owner, Skill skill)
    {
        return !TryComp<SkillsComponent>(owner, out var skillsComponent)
            ? SkillLevel.Weak
            : skillsComponent.Skills[skill].Item1;
    }

    public void SetSkillLevel(EntityUid owner, Skill skill, SkillLevel level)
    {
        if (!TryComp<SkillsComponent>(owner, out var skillsComponent))
            return;

        skillsComponent.Skills[skill] = (level, 0);

        Dirty(owner, skillsComponent);
    }

    public void ImproveSkill(EntityUid owner, Skill skill)
    {
        if (!TryComp<SkillsComponent>(owner, out var skillsComponent))
            return;

        var nextSkill = NextSkillLevel(skillsComponent.Skills[skill].Item1);
        if (skillsComponent.Skills[skill].Item1 == nextSkill)
            return;

        skillsComponent.Skills[skill] = (nextSkill, 0);

        Dirty(owner, skillsComponent);

        _popupSystem.PopupEntity($"My {skill.ToString()} skill grows!", owner, owner);
    }

    public void DecreaseSkill(EntityUid owner, Skill skill)
    {
        if (!TryComp<SkillsComponent>(owner, out var skillsComponent))
            return;

        skillsComponent.Skills[skill] = (PreviousSkillLevel(skillsComponent.Skills[skill].Item1), 0);

        Dirty(owner, skillsComponent);
    }

    public int GetSkillModifier(EntityUid owner, Skill skill)
    {
        var skillLevel = GetSkillLevel(owner, skill);

        return skillLevel switch
        {
            SkillLevel.Weak => 2,
            SkillLevel.Average => 4,
            SkillLevel.Skilled => 6,
            SkillLevel.Master => 8,
            SkillLevel.Legendary => 10,
            _ => 0
        };
    }

    #endregion

    #region Private

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
