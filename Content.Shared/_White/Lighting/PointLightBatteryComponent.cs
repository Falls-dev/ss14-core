using Robust.Shared.GameStates;

namespace Content.Shared._White.Lighting;

[RegisterComponent, NetworkedComponent]
public sealed partial class PointLightBatteryComponent : SharedPointLightComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireBattery = true;
}
