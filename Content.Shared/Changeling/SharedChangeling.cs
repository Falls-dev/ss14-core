using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[Serializable, NetSerializable]
public sealed partial class AbsorbDnaDoAfterEvent: SimpleDoAfterEvent
{
}

public sealed partial class AbsorbDnaActionEvent : EntityTargetActionEvent
{
}

public sealed class AbsorbDnaDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public AbsorbDnaDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class AbsorbDnaDoAfterCancelled : EntityEventArgs
{

}


[Serializable, NetSerializable]
public sealed partial class TransformDoAfterEvent: SimpleDoAfterEvent
{

}

public sealed partial class TransformActionEvent: InstantActionEvent
{

}
