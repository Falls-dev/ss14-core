using Content.Shared._White.Targeting.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Targeting;

[Serializable, NetSerializable]
public sealed class TargetingChangeBodyPartEvent : EntityEventArgs
{
    public NetEntity Entity { get; }
    public TargetingBodyParts BodyPart { get; }
    public TargetingChangeBodyPartEvent(NetEntity entity, TargetingBodyParts bodyPart)
    {
        Entity = entity;
        BodyPart = bodyPart;
    }
}

[Serializable, NetSerializable]
public sealed class TargetingIntegrityChangeEvent : EntityEventArgs
{
    public NetEntity Entity { get; }
    public bool NeedRefresh { get; }
    public TargetingIntegrityChangeEvent(NetEntity entity, bool needRefresh = true)
    {
        Entity = entity;
        NeedRefresh = needRefresh;
    }
}

public sealed class RefreshInventorySlotsEvent : EntityEventArgs
{
    public string SlotName { get; }

    public RefreshInventorySlotsEvent(string slotName)
    {
        SlotName = slotName;
    }
}
