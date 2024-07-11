using Robust.Shared.Prototypes;

namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
public sealed partial class SurveillanceBodyCameraComponent : Component
{
    [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 0.3f;

    // WD EDIT
    [DataField]
    public EntityUid? ToggleActionEntity;

    // WD EDIT
    [DataField]
    public EntProtoId ToggleAction = "ToggleBodyCamera";

    public bool lastState = false;
}

