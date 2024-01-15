using Robust.Shared.Prototypes;

namespace Content.Shared.White.MagGloves;

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

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleMagneticGloves";

    [DataField]
    public string Debugger = "";
}
