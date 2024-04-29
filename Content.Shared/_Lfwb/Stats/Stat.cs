using Robust.Shared.Serialization;

namespace Content.Shared._Lfwb.Stats;

[Serializable, NetSerializable]
public enum Stat : byte
{
    Strength = 0, // affects damage, health, doafters where need str
    Intelligence = 1, // affects examine text, doafters where needs int, machinery
    Dexterity = 2, // affects move speed, attack speed, dodging/parry, hit chance, doafters speedup
    Endurance = 3 // affects stamina
}
