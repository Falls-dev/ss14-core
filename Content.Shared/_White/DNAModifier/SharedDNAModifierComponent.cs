using Robust.Shared.Serialization;

namespace Content.Shared.DNAModifier;

public abstract partial class SharedDNAModifierComponent : Component
{
    [Serializable, NetSerializable]
    public enum DNAModifierVisual : byte
    {
        Status
    }
    [Serializable, NetSerializable]
    public enum DNAModifierStatus : byte
    {
        Off,
        Open,
        Red,
        Death,
        Green,
        Yellow,
    }
}
