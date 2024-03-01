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
}

[Serializable, NetSerializable]
public sealed partial class RemoveWizardChargeEvent : EntityEventArgs
{
}
