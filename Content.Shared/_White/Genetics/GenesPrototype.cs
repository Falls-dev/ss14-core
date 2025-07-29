using Robust.Shared.Prototypes;

namespace Content.Shared._White.Genetics;

/// <summary>
/// Genes for an organism, provides the actual values which are used to build <see cref="Genome"/> bits.
/// </summary>
[Prototype("genes")]
public sealed partial class GenesPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Each bool which will be set to true.
    /// </summary>
    [DataField]
    public HashSet<string> Bools = new();

    /// <summary>
    /// Values of each int to set.
    /// Any unused bits are dropped silently.
    /// </summary>
    [DataField]
    public Dictionary<string, ushort> Ints = new();
}
