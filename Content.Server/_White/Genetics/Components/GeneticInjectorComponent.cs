using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Server._White.Genetics.Components;

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

    [DataField("name")]
    public string? Name;
}

[Serializable, NetSerializable]
public sealed partial class GeneticInjectorDoAfterEvent : SimpleDoAfterEvent
{
}
