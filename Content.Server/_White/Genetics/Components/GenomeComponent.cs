using Content.Shared._White.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Genetics.Components;

/// <summary>
/// Gives this entity a genome for traits to be passed on and potentially mutated.
/// Both of those must be handled by other systems, on its own it has no functionality.
/// TODO: комплиментарные последовательности
/// </summary>
[RegisterComponent]
public sealed partial class GenomeComponent : Component
{
    /// <summary>
    /// Name of the <see cref="GenomePrototype"/> to create on init.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<GenomePrototype> GenomeId = string.Empty;

    /// <summary>
    /// Genome layout to use for this round and genome type.
    /// Stored in <see cref="GeneticsSystem"/>.
    /// </summary>
    [ViewVariables]
    public GenomeLayout Layout = new();

    /// <summary>
    /// Activated mutations. Acquired through activators or radiation.
    /// </summary>
    [ViewVariables]
    public List<string> ActivatedMutations = new List<string>();

    /// <summary>
    /// Mutations that were acquired via mutators.
    /// </summary>
    [DataField]
    public List<string> MutatedMutations = new List<string>();

    /// <summary>
    /// TODO: wtf is this
    /// </summary>
    [DataField("humanGenes")]
    public bool HumanGenes = false;

    /// <summary>
    /// Key is the name of the region in GenomeLayout, value is the name of corresponding mutation.
    /// </summary>
    [DataField]
    public Dictionary<string, string> MutationRegions = new Dictionary<string, string>();

    /// <summary>
    /// Genome bits themselves.
    /// Data can be retrieved with <c>comp.Layout.GetInt(comp.Genome, "name")</c>, etc.
    /// </summary>
    /// <remarks>
    /// Completely empty by default, another system must use <see cref="GenomeSystem"/> to load genes or copy from a parent.
    /// </remarks>
    [DataField]
    public Genome Genome = new();

    /// <summary>
    /// It is changed when a mutation through mutator is applied.
    /// </summary>
    [DataField]
    public int Instability = 0;
}
