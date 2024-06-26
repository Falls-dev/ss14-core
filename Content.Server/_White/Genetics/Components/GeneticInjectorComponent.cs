namespace Content.Server._White.Genetics.Components;

public sealed partial class GeneticInjectorComponent : Component
{
    [DataField("mutatorMutations")]
    public List<string> MutationProtos = new List<string>();

    [DataField("activatorMutations")]
    public List<string> ActivatorMutations = new List<string>();

    [DataField("forced")]
    public bool Forced = false;
}
