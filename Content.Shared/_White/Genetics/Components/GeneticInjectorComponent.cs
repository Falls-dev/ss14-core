using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Genetics.Components;

public sealed partial class GeneticInjectorComponent : Component
{
    [DataField("mutatorMutations")]
    public List<string> MutationProtos = new List<string>();

    [DataField("activatorMutations")]
    public List<string> ActivatorMutations = new List<string>();

    [DataField("forced")]
    public bool Forced = false;

    [DataField("useDelay")]
    public float UseDelay = 2.5f;

    [DataField]
    public bool Used = false;

    /// <summary>
    /// Sprite state to use if Used = false
    /// </summary>
    [DataField]
    public string NewState = "new";

    /// <summary>
    /// Sprite state to use if Used = true
    /// </summary>
    [DataField]
    public string UsedState = "used";
}

[Serializable, NetSerializable]
public sealed partial class GeneticInjectorDoAfterEvent : SimpleDoAfterEvent
{
}
