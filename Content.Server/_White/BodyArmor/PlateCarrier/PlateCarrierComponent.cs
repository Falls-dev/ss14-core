using Robust.Shared.Audio;

namespace Content.Server._White.BodyArmor.PlateCarrier;

[RegisterComponent]
public sealed partial class PlateCarrierComponent : Component
{
    public static string ArmorPlateContainer = "armor_plate";

    [DataField]
    public bool PlateIsClosed = true; // true - застегнут отдел для бронеплит

    [DataField]
    public bool HasPlate = false;

    [DataField]
    public string PlateCarrierSlot = "outerClothing";

    [DataField]
    public bool IsBreak = false;

    [DataField]
    public float ChanceOfBreak = 0.05F;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BreakSound = new SoundPathSpecifier("/Audio/White/BodyArmor/PlateCarrier/break.ogg");

    [DataField]
    public int PlateCarrierDamage = 0;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/White/BodyArmor/PlateCarrier/open.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/White/BodyArmor/PlateCarrier/close.ogg");

    [DataField]
    public TimeSpan TimeToPutPlate = TimeSpan.FromSeconds(3);
}
