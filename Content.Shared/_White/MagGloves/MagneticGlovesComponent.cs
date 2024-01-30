using Robust.Shared.Prototypes;

namespace Content.Shared._White.MagGloves;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class MagneticGlovesComponent : Component
{
    [ViewVariables]
    public bool Enabled { get; set; } = false;

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField("action")]
    public EntProtoId ToggleAction = "ActionToggleMagneticGloves";

    [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 16.5f;

    [DataField]
    public string Debugger = "";
}
