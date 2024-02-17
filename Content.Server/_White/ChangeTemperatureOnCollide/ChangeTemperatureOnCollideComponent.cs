namespace Content.Server._White.ChangeTemperatureOnCollide;

[RegisterComponent]
public sealed class ChangeTemperatureOnCollideComponent : Component
{
    [DataField("temperature"), ViewVariables(VVAccess.ReadWrite)]
    public float Temperature;

    [DataField("fixture")]
    public string FixtureID = "projectile";
}
