using Robust.Shared.Serialization;

namespace Content.Shared._Lfwb.Stats;

[Serializable, NetSerializable]
public enum Stat : byte
{
    Strength = 0, // health
    Intelligence = 1,
    Dexterity = 2, // affects move speed.
    Endurance = 3, // affects stamina
    Luck = 4
}
