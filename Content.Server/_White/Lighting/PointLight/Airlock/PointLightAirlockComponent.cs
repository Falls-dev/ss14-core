namespace Content.Server._White.Lighting.PointLight.Airlock;

[RegisterComponent]
public sealed partial class PointLightAirlockComponent : Component
{
    [ViewVariables, DataField]
    public bool RequirePower = true;

    [ViewVariables]
    public string BoltedColor = "#C07F7F";

    [ViewVariables]
    public string PoweredColor = "#7F93C0";

    [ViewVariables]
    public string EmergencyLightsColor = "#BDC07F";

    [ViewVariables]
    public string OpeningColor = "#7FC080";
}
