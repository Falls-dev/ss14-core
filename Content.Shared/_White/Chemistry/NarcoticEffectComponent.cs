using System.Threading;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Chemistry;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class NarcoticEffectComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> TimerInterval = new() { 7800, 11000, 9500, 12000, 10000 };

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public List<int> SlurTime = new() { 35, 60, 80, 90, 45 };
}

[Serializable, NetSerializable]
public enum NarcoticEffects
{
    Stun,
    Tremor,
    Shake,
    TremorAndShake,
    StunAndShake
}
