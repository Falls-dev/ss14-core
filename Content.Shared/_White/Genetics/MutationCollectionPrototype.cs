using Robust.Shared.Prototypes;

namespace Content.Server.Genetics
{
    /// <summary>
    /// This is a prototype for a mutation pool.
    /// </summary>
    [Prototype("mutationCollection")]
    public sealed partial class MutationCollectionPrototype : IPrototype
    {
        /// <inheritdoc/>
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        /// List of Ids of mutations in the collection.
        /// </summary>
        [DataField("mutations", required: true)]
        public IReadOnlyList<string> Mutations { get; private set; } = Array.Empty<string>();
    }
}
