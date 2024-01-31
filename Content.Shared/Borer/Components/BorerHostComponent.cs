using Robust.Shared.GameStates;

namespace Content.Shared.Borer;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BorerHostComponent : Component
{
    public EntityUid Borer;
}
