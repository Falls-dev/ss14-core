using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;


[RegisterComponent,NetworkedComponent]
public sealed partial class ChangelingComponent: Component
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

    [ViewVariables(VVAccess.ReadOnly), DataField("absorbedEntities")]
    public Dictionary<string, HumanoidData> AbsorbedEntities = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNACost")]
    public int AbsorbDnaCost;

    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNADelay")]
    public float AbsorbDnaDelay = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("TransformDelay")]
    public float TransformDelay = 3f;

    [DataField]
    public EntityUid? AbsorbAction;

    [DataField]
    public EntityUid? TransformAction;

}

public struct HumanoidData
{
    public EntityPrototype? EntityPrototype;

    public MetaDataComponent? MetaDataComponent;

    public HumanoidAppearanceComponent AppearanceComponent;

    public string Name;

    public EntityUid? EntityUid;
}
