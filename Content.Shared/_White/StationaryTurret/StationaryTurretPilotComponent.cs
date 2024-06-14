using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._White.StationaryTurret;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationaryTurretPilotComponent : Component
{
    [ViewVariables]
    public EntityCoordinates? Position { get; set; }

    [ViewVariables, AutoNetworkedField]
    public EntityUid Turret;
}

