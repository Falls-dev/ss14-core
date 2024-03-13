using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

[Serializable, NetSerializable]
public sealed partial class PatchDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Implements draw/inject behavior for droppers and syringes.
/// </summary>
/// <remarks>
/// Can optionally support both
/// injection and drawing or just injection. Can inject/draw reagents from solution
/// containers, and can directly inject into a mobs bloodstream.
/// </remarks>

[NetworkedComponent()]
public abstract partial class SharedPatchComponent : Component
{
    [DataField("solutionName")]
    public string SolutionName = "patch";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);
}

[Serializable, NetSerializable]
public sealed class PatchComponentState : ComponentState
{
    public FixedPoint2 CurVolume { get; }
    public FixedPoint2 MaxVolume { get; }

    public PatchComponentState(FixedPoint2 curVolume, FixedPoint2 maxVolume)
    {
        CurVolume = curVolume;
        MaxVolume = maxVolume;
    }
}
