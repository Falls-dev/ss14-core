using Robust.Shared.GameStates;

namespace Content.Shared._White.Executions;

[RegisterComponent, NetworkedComponent]
public sealed partial class ExecutionComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float Cooldown = 0F;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextAttempt = TimeSpan.FromSeconds(3);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUse = TimeSpan.Zero;
}
