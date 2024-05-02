using Robust.Shared.Serialization;

namespace Content.Shared._Lfwb.Stats;

[Serializable, NetSerializable]
public enum Stat : byte
{
    Strength = 0,
    Intelligence = 1,
    Dexterity = 2, // affects move speed.
    Endurance = 3,
    Luck = 4
}

[ByRefEvent]
public record struct StatChangedEvent(EntityUid Owner, Stat Stat, int OldValue, int NewValue, bool Init);
