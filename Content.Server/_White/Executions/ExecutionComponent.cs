namespace Content.Server._White.Executions;

[RegisterComponent]
public sealed partial class ExecutionComponent : Component
{
    [DataField]
    public float DoAfterDuration = 5f;

    [DataField]
    public float DamageModifier = 9f;

    [DataField]
    public bool Executing = true;
}
