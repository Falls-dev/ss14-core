namespace Content.Server._White.BodyArmor.PlateCarrier;

[RegisterComponent]
public sealed partial class PlateCarrierOnUserComponent : Component
{
    [DataField]
    public EntityUid? PlateCarrier;
}
