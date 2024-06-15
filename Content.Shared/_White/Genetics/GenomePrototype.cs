using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics;

/// <summary>
/// Genome for an organism.
/// Internally represented as a bit array, shown to players as bases using <see cref="GetBases"/>.
/// Other systems can get data using <see cref="GetBool"/> and <see cref="GetInt"/>.
/// Each bit can either be a boolean or be part of a number, which has its bits stored sequentially.
/// Each species has its own unique genome layout that maps bits to data, which is randomized roundstart.
/// Variable length information such as a list of reagents cannot be stored here.
/// </summary>
[Prototype("genome")]
public sealed partial class GenomePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Names and bit lengths of each value in the genome.
    /// If length is 1 then it will be a bool.
    /// </summary>
    [DataField("valuebits", required: true)]
    public Dictionary<string, ushort> ValueBits = new();
}
