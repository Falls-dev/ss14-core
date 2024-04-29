using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared._Lfwb.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    #region Data

    public static Dictionary<SkillLevel, float> SkillLevelToDelay = new()
    {
        { SkillLevel.Weak , 6f},
        { SkillLevel.Average , 4f},
        { SkillLevel.Skilled , 2f},
        { SkillLevel.Master , 1f},
        { SkillLevel.Legendary , 0.5f},
    };

    public static Dictionary<SkillLevel, float> SkillLevelToChance = new()
    {
        { SkillLevel.Weak , 0.2f},
        { SkillLevel.Average , 0.4f},
        { SkillLevel.Skilled , 0.6f},
        { SkillLevel.Master , 0.8f},
        { SkillLevel.Legendary , 1f},
    };

    public static Dictionary<SkillLevel, int> SkillLevelToAddition = new()
    {
        { SkillLevel.Weak , 0},
        { SkillLevel.Average , 4},
        { SkillLevel.Skilled , 6},
        { SkillLevel.Master , 8},
        { SkillLevel.Legendary , 10},
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

    public bool Roller(EntityUid owner, Skill skill)
    {
        if (!TryComp<SkillsComponent>(owner, out var skillsComponent))
            return false;

        var skillLevel = skillsComponent.Skills[skill].Item1;
        var chance = SkillLevelToChance[skillLevel];

        return _robustRandom.Prob(chance);
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
