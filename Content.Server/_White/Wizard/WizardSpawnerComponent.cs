using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Wizard;


[RegisterComponent]
public sealed partial class WizardSpawnerComponent : Component
{
    [DataField("name")]
    public string Name = "Ololo, the Balls' Twister";

    [DataField("points")]
    public int Points = 10;

    [DataField("startingGear")]
    public ProtoId<StartingGearPrototype> StartingGear = "WizardStartingGear";

    [DataField]
    public ProtoId<AntagPrototype> WizardRoleProto = "WizardRole";
}
