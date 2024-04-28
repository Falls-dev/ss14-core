using Robust.Shared.Serialization;

namespace Content.Shared._Lfwb.Stats;

[Serializable, NetSerializable]
public enum Stat : byte
{
    Strength, // affects damage, health
    Intelligence, // affects something
    Dexterity, // affects move speed, attack speed, dodging/parry, hit chance
    Endurance // affects stamina
}
