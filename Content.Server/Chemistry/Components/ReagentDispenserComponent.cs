using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// A machine that dispenses reagents into a solution container.
/// </summary>
[RegisterComponent, Access(typeof(ReagentDispenserSystem), typeof(ChemMasterSystem))]
public sealed partial class ReagentDispenserComponent : Component
{
    [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>)),
     ViewVariables(VVAccess.ReadWrite)]
    public string? PackPrototypeId;

    [DataField("emagPack", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>)),
     ViewVariables(VVAccess.ReadWrite)]
    public string? EmagPackPrototypeId;

    [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public ReagentDispenserDispenseAmount DispenseAmount = ReagentDispenserDispenseAmount.U10;

    // WD STAR
    public const string ChemMasterPort = "ChemMasterSender";

    [ViewVariables]
    public FixedPoint2 Charge = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxCharge = 300;

    [ViewVariables(VVAccess.ReadWrite)]
    public float EmaggedMaxCharge = 1000;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RechargeRate = 5f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float EmaggedRegenRate = 15f;

    [ViewVariables]
    public bool ChemMasterInRange;

    [ViewVariables]
    public EntityUid? ChemMaster;
    // WD END
}
