using Robust.Shared.GameStates;

namespace Content.Shared._White.Executions;

[RegisterComponent, NetworkedComponent]
public sealed partial class ExecutionComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public TimeSpan Delay = TimeSpan.Zero;
}
