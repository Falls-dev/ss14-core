using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._White.Sponsors;

[Prototype("sponsorLoadout")]
public sealed class SponsorLoadoutItemPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("entity", required: true)]
    public EntProtoId EntityId { get; }

    // WD-Sponsors-Start
    [DataField("sponsorOnly")]
    public bool SponsorOnly = false;
    // WD-Sponsors-End

    [DataField("whitelistJobs")]
    public List<ProtoId<JobPrototype>>? WhitelistJobs { get; }

    [DataField("blacklistJobs")]
    public List<ProtoId<JobPrototype>>? BlacklistJobs { get; }

    [DataField("speciesRestriction")]
    public List<ProtoId<SpeciesPrototype>>? SpeciesRestrictions { get; }
}

