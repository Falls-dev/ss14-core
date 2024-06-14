using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.StationaryTurret;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationaryTurretComponent : Component
{

    [DataField("zoom")]
    public Vector2 Zoom = new(1.5f, 1.5f);

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid Pilot;

    [DataField]
    public EntityWhitelist? PilotWhitelist;

    [DataField]
    public EntityUid? TurretEjectActionEntity;

    [DataField]
    public EntProtoId TurretEjectAction = "ActionTurretEject";

}
