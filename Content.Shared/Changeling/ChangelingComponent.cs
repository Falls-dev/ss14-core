using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;


[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public string StoreCurrencyName = "Points";

    [ViewVariables(VVAccess.ReadOnly)]
    public string AbilityCurrencyName = "Chemicals";

    [DataField("startingPoints")]
    public int StartingPoints = 10;

    [DataField("chemRegenRate")]
    public int ChemicalRegenRate = 1;

    [DataField("chemRegenTime")]
    public float ChemicalRegenTime = 2f;

    [DataField("chemicalCap")]
    public int ChemicalCapacity = 75;

    [DataField("chemicalsBalance")]
    public int ChemicalsBalance;

    [DataField("pointsBalance")]
    public int PointsBalance;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsRegenerating;

    [ViewVariables(VVAccess.ReadOnly), DataField("absorbedEntities")]
    public Dictionary<string, HumanoidData> AbsorbedEntities = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNACost")]
    public int AbsorbDnaCost;

    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNADelay")]
    public float AbsorbDnaDelay = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("TransformDelay")]
    public float TransformDelay = 2f;

    [ViewVariables(VVAccess.ReadWrite), DataField("RegenerateDelay")]
    public float RegenerateDelay = 20f;

    [DataField]
    public EntityUid? AbsorbAction;

    [DataField]
    public EntityUid? TransformAction;

    [DataField]
    public EntityUid? RegenerateAction;

}

public struct HumanoidData
{
    public EntityPrototype? EntityPrototype;

    public MetaDataComponent? MetaDataComponent;

    public HumanoidAppearanceComponent AppearanceComponent;

    public string Name;

    public string Dna;

    public EntityUid? EntityUid;
}
