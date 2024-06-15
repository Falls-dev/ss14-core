using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Genetics;

/// <summary>
/// This is a prototype for mutation
/// </summary>
[Prototype(type: "mutation"), PublicAPI]
public sealed partial class MutationPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Instability that brings this mutation if add via mutator.
    /// </summary>
    [DataField("instability", required: false)]
    public int Instability { get; private set; } = 0;

    /// <summary>
    /// Length of genetic sequence for this mutation.
    /// </summary>
    [DataField("length", required: true)]
    public int Length { get; private set; } = 64;
}
