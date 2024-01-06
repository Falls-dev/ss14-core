using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[Serializable, NetSerializable]
public sealed partial class AbsorbDnaDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class AbsorbDnaActionEvent : EntityTargetActionEvent
{
}


[Serializable, NetSerializable]
public sealed partial class TransformDoAfterEvent : SimpleDoAfterEvent
{
    public string SelectedDna;
}

public sealed partial class TransformActionEvent : InstantActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class RegenerateDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class RegenerateActionEvent : InstantActionEvent
{

}

