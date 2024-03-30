using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

[RegisterComponent]
public sealed partial class SpawnPointComponent : Component
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("job_id")]
    private string? _jobId;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("spawn_type")]
    public SpawnPointType SpawnType { get; private set; } = SpawnPointType.Unset;

    /// <summary>
    /// WD EDIT. This is needed for Violence gamemode for team spawners.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("teamId")]
    public ushort? TeamId = null;

    public JobPrototype? Job => string.IsNullOrEmpty(_jobId) ? null : _prototypeManager.Index<JobPrototype>(_jobId);

    public override string ToString()
    {
        return $"{_jobId} {SpawnType}";
    }
}

public enum SpawnPointType
{
    Unset = 0,
    LateJoin,
    Job,
    Observer,
}
