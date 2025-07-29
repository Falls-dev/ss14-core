using Robust.Shared.Serialization;

namespace Content.Shared._White.Genetics;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class DNAScannerScannedGenomeMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public Genome Genome;
    public GenomeLayout Layout;
    public Dictionary<string, string> MutationRegions;
    public List<string> MutatedMutations;
    public List<string> ActivatedMutations;
    public string? FingerPrints;

    public DNAScannerScannedGenomeMessage(NetEntity? targetEntity,
        Genome genome,
        GenomeLayout layout,
        Dictionary<string, string> mutationRegions,
        List<string> mutatedMutations,
        List<string> activatedMutations,
        string? fingerPrints)
    {
        TargetEntity = targetEntity;
        Genome = genome;
        Layout = layout;
        MutationRegions = mutationRegions;
        MutatedMutations = mutatedMutations;
        ActivatedMutations = activatedMutations;
        FingerPrints = fingerPrints;
    }
}
