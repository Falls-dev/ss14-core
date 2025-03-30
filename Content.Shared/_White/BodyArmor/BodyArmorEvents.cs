using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.BodyArmor;

[Serializable, NetSerializable]
public sealed partial class PutPlateDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class GetPlateDoAfterEvent : SimpleDoAfterEvent
{
}
