using Robust.Shared.Serialization;

namespace Content.Shared._Lfwb.Stats;

[Serializable, NetSerializable]
public enum Stat : byte
{
    Strength = 0, // affects damage, health
    Intelligence = 1, // affects something
    Dexterity = 2, // affects move speed, attack speed, dodging/parry, hit chance
    Endurance = 3 // affects stamina
}
