using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._White.FluffSystems.PetSummonSystem;

[RegisterComponent]
public sealed partial class PetSummonComponent : Component
{
    [DataField("petSummonAction")]
    public EntProtoId PetSummonAction = "PetSummonAction";

    [DataField("petGhostSummonAction")]
    public EntProtoId PetGhostSummonAction = "PetGhostSummonAction";

    [DataField("petSummonActionEntity")]
    public EntityUid? PetSummonActionEntity;

    [DataField("petGhostSummonActionEntity")]
    public EntityUid? PetGhostSummonActionEntity;

    public int UsesLeft = 10;

    public EntityUid? SummonedEntity;
}
