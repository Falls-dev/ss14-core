using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Map;
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
    /// Min players needed for Violence round to start.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = 2;

    /// <summary>
    /// Max players needed for Violence round.
    /// </summary>
    [DataField("maxPlayers"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ushort, int> MaxPlayers = new Dictionary<ushort, int>();

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
    [DataField("teamScores"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ushort, int> TeamScores { get; private set; } = new Dictionary<ushort, int>();

    List<int> kd = new List<int>(new int[2]);
    /// <summary>
    /// Stores number of kills and deaths for each player. Indexes: 0 for kills, 1 for assists, 2 for deaths.
    /// </summary>
    [DataField("killsDeaths"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<NetUserId, List<int>> KillDeaths { get; private set; } = new Dictionary<NetUserId, List<int>>();

    /// <summary>
    /// Stored, for some reason.
    /// </summary>
    [DataField("matchVictor"), ViewVariables(VVAccess.ReadWrite)]
    public ushort? MatchVictor;

    /// <summary>
    /// The number of points a player has to get to win.
    /// </summary>
    [DataField("pointCap"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PointCap = 5;

    /// <summary>
    /// Time when current round ends.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RoundEndTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Time between final kill of the round and actual round end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RoundEndDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Time when new round starts.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RoundStartTime = TimeSpan.Zero;

    /// <summary>
    /// Time between player's spawns and round start.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RoundStartDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The duration of a round.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RoundDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Dictionary of a players and their teams
    /// </summary>
    [DataField("teamMembers"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<NetUserId, ushort> TeamMembers { get; private set; } = new Dictionary<NetUserId, ushort>();

    /// <summary>
    /// Dictionary of a players and their money.
    /// </summary>
    [DataField("money"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<NetUserId, int> Money { get; private set; } = new Dictionary<NetUserId, int>();

    /// <summary>
    /// Dictionary of a players and lists of their equipment.
    /// </summary>
    [DataField("savedEquip"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<NetUserId, List<EntityUid>> SavedEquip { get; private set; } = new Dictionary<NetUserId, List<EntityUid>>();

    /// <summary>
    /// This tries to save the slots of player's equip from the last round.
    /// </summary>
    [DataField("equipSlots"), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<EntityUid, string> EquipSlots { get; private set; } = new Dictionary<EntityUid, string>();

    /// <summary>
    /// Reward for remaining alive at the end of the round.
    /// </summary>
    [DataField("aliveReward"), ViewVariables(VVAccess.ReadWrite)]
    public int AliveReward { get; private set; } = 300;

    /// <summary>
    /// Reward for remaining alive at the end of the round.
    /// </summary>
    [DataField("killReward"), ViewVariables(VVAccess.ReadWrite)]
    public int KillReward { get; private set; } = 300;


    /// <summary>
    /// Pool of maps for this set of teams
    /// </summary>
    [DataField("mapPool"), ViewVariables(VVAccess.ReadWrite)]
    public string MapPool;

    /// <summary>
    /// EntityUid of current map.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public MapId? CurrentMap = null;

    [DataField("roundState"), ViewVariables(VVAccess.ReadWrite)]
    public RoundState RoundState { get; set; } = RoundState.NotInProgress;
}

public enum RoundState
{
    Starting,
    InProgress,
    NotInProgress
}
