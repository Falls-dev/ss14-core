using Robust.Shared.GameStates;

namespace Content.Shared._White.Executions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExecutionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DoAfterDuration = 5f;

    [DataField, AutoNetworkedField]
    public float DamageModifier = 9f;

    [DataField]
    public bool Executing = true;
}
