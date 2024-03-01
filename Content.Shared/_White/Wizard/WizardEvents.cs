using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Wizard;

[Serializable, NetSerializable]
public sealed partial class ScrollDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class AddWizardChargeEvent : EntityEventArgs
{
    public string ChargeProto;

    public AddWizardChargeEvent(string chargeProto)
    {
        ChargeProto = chargeProto;
    }
}

[Serializable, NetSerializable]
public sealed partial class RemoveWizardChargeEvent : EntityEventArgs
{
}
