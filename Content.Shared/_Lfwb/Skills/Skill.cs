using Robust.Shared.Serialization;

namespace Content.Shared._Lfwb.Skills;

[Serializable, NetSerializable]
public enum Skill : byte
{
    Melee = 0, // done
    Ranged = 1,
    Medicine = 2, // done
    Engineering = 3
}

[Serializable, NetSerializable]
public enum SkillLevel : byte
{
    Weak = 0,
    Average = 1,
    Skilled = 2,
    Master = 3,
    Legendary = 4
}
