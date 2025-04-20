namespace Content.Shared.Body.Part;

[ByRefEvent]
public readonly record struct BodyPartAddedEvent(string Slot, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct BodyPartRemovedEvent(string Slot, Entity<BodyPartComponent> Part);

[ByRefEvent]
public readonly record struct TargetingBodyPartEnableChangedEvent(bool IsEnabled);

[ByRefEvent]
public readonly record struct TargetingBodyPartEnabledEvent(EntityUid Entity, BodyPartComponent BodyPartComponent);

[ByRefEvent]
public readonly record struct TargetingBodyPartDisabledEvent(EntityUid Entity, BodyPartComponent BodyPartComponent);
