using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._Miracle.GameRules;

[RegisterComponent]
[Access(typeof(ViolenceRuleSystem))]
public sealed partial class ViolenceRuleComponent : Component
{
    /// <summary>
    /// Min players needed for Violence gamerule to start.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = 2;

    /// <summary>
    /// How long until the round restarts
    /// </summary>
    [DataField("restartDelay"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RestartDelay = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// Time until automatic match end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? MatchDuration = null;

    /// <summary>
    /// Time until automatic match end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? MatchStartTime = null;

    /// <summary>
    /// List of teams in this gamemode.
    /// </summary>
    [DataField("teams")]
    public IReadOnlyList<ushort> Teams { get; private set; } = Array.Empty<ushort>();

    /// <summary>
    /// List of scores in this gamemode.
    /// </summary>
    [DataField("scores")]
    public Dictionary<ushort, int> Scores { get; private set; } = new Dictionary<ushort, int>();

    /// <summary>
    /// Stored, for some reason.
    /// </summary>
    [DataField("victor")]
    public ushort? Victor;

    /// <summary>
    /// The number of points a player has to get to win.
    /// </summary>
    [DataField("pointCap"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PointCap = 100;

    /// <summary>
    /// Dictionary of a players and their teams
    /// </summary>
    [DataField("teamMembers")]
    public Dictionary<NetUserId, ushort> TeamMembers { get; private set; } = new Dictionary<NetUserId, ushort>();
}
