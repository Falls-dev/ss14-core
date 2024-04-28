namespace Content.Shared._Lfwb.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    #region Data

    public static Dictionary<SkillLevel, TimeSpan> SkillLevelToDelay = new()
    {
        { SkillLevel.Weak , TimeSpan.FromSeconds(6)},
        { SkillLevel.Average , TimeSpan.FromSeconds(4)},
        { SkillLevel.Skilled , TimeSpan.FromSeconds(2)},
        { SkillLevel.Master , TimeSpan.FromSeconds(1)},
        { SkillLevel.Legendary , TimeSpan.FromSeconds(0.5)},
    };

    public static Dictionary<SkillLevel, float> SkillLevelToChance = new()
    {
        { SkillLevel.Weak , 0.2f},
        { SkillLevel.Average , 0.4f},
        { SkillLevel.Skilled , 0.6f},
        { SkillLevel.Master , 0.8f},
        { SkillLevel.Legendary , 1f},
    };

    #endregion
}
