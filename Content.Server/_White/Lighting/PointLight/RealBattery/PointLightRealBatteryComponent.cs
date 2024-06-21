using Robust.Shared.GameStates;

namespace Content.Server._White.Lighting.PointLight.RealBattery;

[RegisterComponent, NetworkedComponent]
public sealed partial class PointLightRealBatteryComponent : Component
{
    [DataField, ViewVariables]
    public string RedColor = "#D56C6C";

    [DataField, ViewVariables]
    public string GreenColor = "#7FC080";

    [DataField, ViewVariables]
    public string YellowColor = "#BDC07F";

}
