using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Miracle.GulagSystem;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server._White;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Server._White.PandaSocket.Main;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Managers;
// TODO WD Fix
public sealed partial class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!; // WD
    [Dependency] private readonly PandaWebManager _pandaWeb = default!; // WD
    [Dependency] private readonly IEntityManager _entMan = default!; // WD
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbData = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.bans";
    public const string JobPrefix = "Job:";
    public const string UnknownServer = "unknown"; // WD

    private readonly Dictionary<NetUserId, HashSet<ServerBanDef>> _cachedServerBans = new(); // Miracle edit
    private readonly Dictionary<ICommonSession, List<ServerRoleBanDef>> _cachedRoleBans = new();
    // Cached ban exemption flags are used to handle
    private readonly Dictionary<ICommonSession, ServerBanExemptFlags> _cachedBanExemptions = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgRoleBans>();

        _db.SubscribeToNotifications(OnDatabaseNotification);

        _userDbData.AddOnLoadPlayer(CachePlayerData);
        _userDbData.AddOnPlayerDisconnect(ClearPlayerData);
    }

    private async Task CachePlayerData(ICommonSession player, CancellationToken cancel)
    {
        var flags = await _db.GetBanExemption(player.UserId, cancel);

        var netChannel = player.Channel;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        await CacheDbServerBans(e.Session.UserId, netChannel.RemoteEndPoint.Address, hwId); //Miracle edit
        var modernHwids = netChannel.UserData.ModernHWIds;
        var roleBans = await _db.GetServerRoleBansAsync(netChannel.RemoteEndPoint.Address, player.UserId, hwId, modernHwids, false);

        var userRoleBans = new List<ServerRoleBanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        cancel.ThrowIfCancellationRequested();
        _cachedBanExemptions[player] = flags;
        _cachedRoleBans[player] = userRoleBans;

        SendRoleBans(player);
    }

    private void ClearPlayerData(ICommonSession player)
    {
        _cachedBanExemptions.Remove(player);
    }

    private async Task<bool> AddRoleBan(ServerRoleBanDef banDef)
    {
        banDef = await _db.AddServerRoleBanAsync(banDef);

        if (banDef.UserId != null
            && _playerManager.TryGetSessionById(banDef.UserId, out var player)
            && _cachedRoleBans.TryGetValue(player, out var cachedBans))
        {
            cachedBans.Add(banDef);
        }

        return true;
    }

    public HashSet<string>? GetRoleBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return _cachedRoleBans.TryGetValue(session, out var roleBans)
            ? roleBans.Select(banDef => banDef.Role).ToHashSet()
            : null;
    }

    //Miracle edit start
    private async Task CacheDbServerBans(NetUserId userId, IPAddress? address = null, ImmutableArray<byte>? hwId = null)
    {
        var serverBans = await _db.GetServerBansAsync(address, userId, hwId, false, _cfg.GetCVar(CCVars.AdminLogsServerName));

        var userServerBans = new HashSet<ServerBanDef>(serverBans);

        _cachedServerBans[userId] = userServerBans;
    }
    //Miracle edit end

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new ValueList<ICommonSession>();
        foreach (var player in _cachedRoleBans.Keys.Concat(_cachedServerBans.Keys)) // Miracle edit
        {
            if (player.Status == SessionStatus.Disconnected)
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
            _cachedServerBans.Remove(player); //Miracle edit
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }

        //Miracle edit start
        foreach (var serverBan in _cachedServerBans.Values)
        {
            serverBan.RemoveWhere(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
        //Miracle edit end
    }

    #region Server Bans
    public async void CreateServerBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, uint? minutes, NoteSeverity severity, string reason, bool isGlobalBan)
    {
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        var serverName = _cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = UnknownServer;
        }

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerBanDef(
            null,
            target,
            addressRange,
            hwid,
            DateTimeOffset.Now,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            serverName);

        await _db.AddServerBanAsync(banDef);
        var adminName = banningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = target is null ? "null" : $"{targetUsername} ({target})";
        var addressRangeString = addressRange != null
            ? $"{addressRange.Value.Item1}/{addressRange.Value.Item2}"
            : "null";
        var hwidString = hwid?.ToString() ?? "null";
        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", reason));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        //WD start
        var dbMan = IoCManager.Resolve<IServerDbManager>();
        var listban = await dbMan.GetServerBansAsync(null, target, null);
        var banId = listban.Count == 0 ? null : listban[^1].Id;

        var utkaBanned = new UtkaBannedEvent()
        {
            Ckey = targetUsername,
            ACkey = adminName,
            Bantype = "server",
            Duration = minutes,
            Global = isGlobalBan,
            Reason = reason,
            Rid = EntitySystem.Get<GameTicker>().RoundId,
            BanId = banId
        };
        _pandaWeb.SendBotPostMessage(utkaBanned);
        _entMan.EventBus.RaiseEvent(EventSource.Local, utkaBanned);
        //WD end


        if (banDef.UserId.HasValue)
        {
            var banlist = await _db.GetServerBansAsync(addressRange?.Item1, target, hwid, false);
            if (banlist.Count > 0 && banlist[^1].Id.HasValue)
            {
                var banDefWithId = new ServerBanDef(
                    banlist[^1].Id,
                    banDef.UserId,
                    banDef.Address,
                    banDef.HWId,
                    banDef.BanTime,
                    banDef.ExpirationTime,
                    banDef.RoundId,
                    banDef.PlaytimeAtNote,
                    banDef.Reason,
                    banDef.Severity,
                    banDef.BanningAdmin,
                    banDef.Unban,
                    banDef.ServerName);

                _cachedServerBans.GetOrNew(banDef.UserId.Value).Add(banDefWithId);
            }
        }

        // If we're not banning a player we don't care about disconnecting people
        if (target == null)
            return;

        // Is the player connected?
        if (!_playerManager.TryGetSessionById(target.Value, out var targetPlayer))
            return;
        // Kick when perma
        if (banDef.ExpirationTime == null)
        {
            var message = banDef.FormatBanMessage(_cfg, _localizationManager);
            targetPlayer.Channel.Disconnect(message);
        }
        else // Teleport to gulag
        {
            var gulag = _systems.GetEntitySystem<GulagSystem>();
            gulag.SendToGulag(targetPlayer);
        }

        //KickMatchingConnectedPlayers(banDef, "newly placed ban");
    }

    private void KickMatchingConnectedPlayers(ServerBanDef def, string source)
    {
        foreach (var player in _playerManager.Sessions)
        {
            if (BanMatchesPlayer(player, def))
            {
                KickForBanDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private bool BanMatchesPlayer(ICommonSession player, ServerBanDef ban)
    {
        var playerInfo = new BanMatcher.PlayerInfo
        {
            UserId = player.UserId,
            Address = player.Channel.RemoteEndPoint.Address,
            HWId = player.Channel.UserData.HWId,
            ModernHWIds = player.Channel.UserData.ModernHWIds,
            // It's possible for the player to not have cached data loading yet due to coincidental timing.
            // If this is the case, we assume they have all flags to avoid false-positives.
            ExemptFlags = _cachedBanExemptions.GetValueOrDefault(player, ServerBanExemptFlags.All),
            IsNewPlayer = false,
        };

        return BanMatcher.BanMatches(ban, playerInfo);
    }

    private void KickForBanDef(ICommonSession player, ServerBanDef def)
    {
        var message = def.FormatBanMessage(_cfg, _localizationManager);
        player.Channel.Disconnect(message);
    }

    #endregion

    //Miracle edit start
    public HashSet<ServerBanDef> GetServerBans(NetUserId userId)
    {
        if (_cachedServerBans.TryGetValue(userId, out var bans))
        {
            return bans;
        }

        return new HashSet<ServerBanDef>();
    }

    public void RemoveCachedServerBan(NetUserId userId, int? id)
    {
        if (_cachedServerBans.TryGetValue(userId, out var bans))
        {
            bans.RemoveWhere(ban => ban.Id == id);
        }
    }

    public void AddCachedServerBan(ServerBanDef banDef)
    {
        if (banDef.UserId == null)
            return;

        _cachedServerBans.GetOrNew(banDef.UserId.Value).Add(banDef);
    }
    //Miracle edit end

    #region Job Bans
    // If you are trying to remove timeOfBan, please don't. It's there because the note system groups role bans by time, reason and banning admin.
    // Removing it will clutter the note list. Please also make sure that department bans are applied to roles with the same DateTimeOffset.
    public async void CreateRoleBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableTypedHwid? hwid, string role, uint? minutes, NoteSeverity severity, string reason, DateTimeOffset timeOfBan, bool isGlobalBan)
    {
        if (!_prototypeManager.TryIndex(role, out JobPrototype? _))
        {
            throw new ArgumentException($"Invalid role '{role}'", nameof(role));
        }

        role = string.Concat(JobPrefix, role);
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        var serverName = _cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = UnknownServer;
        }

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerRoleBanDef(
            null,
            target,
            addressRange,
            hwid,
            timeOfBan,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            role,
            serverName);

        if (!await AddRoleBan(banDef))
        {
            _chat.SendAdminAlert(Loc.GetString("cmd-roleban-existing", ("target", targetUsername ?? "null"), ("role", role)));
            return;
        }

        var length = expires == null ? Loc.GetString("cmd-roleban-inf") : Loc.GetString("cmd-roleban-until", ("expires", expires));
        _chat.SendAdminAlert(Loc.GetString("cmd-roleban-success", ("target", targetUsername ?? "null"), ("role", role), ("reason", reason), ("length", length)));

        if (target != null && _playerManager.TryGetSessionById(target.Value, out var session))
        {
            SendRoleBans(session);
        }
    }

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetServerRoleBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddServerRoleUnbanAsync(new ServerRoleUnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        if (ban.UserId is { } player
            && _playerManager.TryGetSessionById(player, out var session)
            && _cachedRoleBans.TryGetValue(session, out var roleBans))
        {
            roleBans.RemoveAll(roleBan => roleBan.Id == ban.Id);
            SendRoleBans(session);
        }

        return $"Pardoned ban with id {banId}";
    }

    public HashSet<ProtoId<JobPrototype>>? GetJobBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        if (!_cachedRoleBans.TryGetValue(session, out var roleBans))
            return null;

        return roleBans
            .Where(ban => ban.Role.StartsWith(JobPrefix, StringComparison.Ordinal))
            .Select(ban => new ProtoId<JobPrototype>(ban.Role[JobPrefix.Length..]))
            .ToHashSet();
    }
    #endregion

    public void SendRoleBans(ICommonSession pSession)
    {
        var roleBans = _cachedRoleBans.GetValueOrDefault(pSession) ?? new List<ServerRoleBanDef>();
        var bans = new MsgRoleBans()
        {
            Bans = roleBans.Select(o => o.Role).ToList()
        };

        _sawmill.Debug($"Sent rolebans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }

    //WD start
    public async void UtkaCreateDepartmentBan(string admin, string target, DepartmentPrototype department, string reason, uint minutes, bool isGlobalBan,
        IPandaStatusHandlerContext context)
    {
        var located = await _playerLocator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddress = located.LastAddress;

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
        }

        (IPAddress, int)? addressRange = null;
        if (targetAddress != null)
        {
            if (targetAddress.IsIPv4MappedToIPv6)
                targetAddress = targetAddress.MapToIPv4();

            // Ban /64 for IPv4, /32 for IPv4.
            var cidr = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 64 : 32;
            addressRange = (targetAddress, cidr);
        }

        var cfg = UnsafePseudoIoC.ConfigurationManager;
        var serverName = cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = "unknown";
        }

        var locatedPlayer = await _playerLocator.LookupIdByNameOrIdAsync(admin);
        if (locatedPlayer == null)
        {
            UtkaSendResponse(false, context);
            return;
        }
        var player = locatedPlayer.UserId;

        UtkaSendResponse(true, context);

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = (await _db.GetPlayTimes(targetUid)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        foreach (var job in department.Roles)
        {
            var role = string.Concat(JobPrefix, job);

            var banDef = new ServerRoleBanDef(
                null,
                targetUid,
                addressRange,
                targetHWid,
                DateTimeOffset.Now,
                expires,
                roundId,
                playtime,
                reason,
                NoteSeverity.High,
                player,
                null,
                role,
                serverName);

            if (!await AddRoleBan(banDef))
                continue;

            var banId = await UtkaGetBanId(reason, role, targetUid);

            UtkaSendJobBanEvent(admin, target, minutes, job, isGlobalBan, reason, banId);
        }

        SendRoleBans(located);
    }

    public async void UtkaCreateJobBan(string admin, string target, string job, string reason, uint minutes, bool isGlobalBan,
        IPandaStatusHandlerContext context)
    {
        if (!_prototypeManager.TryIndex<JobPrototype>(job, out _))
        {
            UtkaSendResponse(false, context);
            return;
        }

        var role = string.Concat(JobPrefix, job);

        var located = await _playerLocator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddress = located.LastAddress;

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
        }

        (IPAddress, int)? addressRange = null;
        if (targetAddress != null)
        {
            if (targetAddress.IsIPv4MappedToIPv6)
                targetAddress = targetAddress.MapToIPv4();

            // Ban /64 for IPv4, /32 for IPv4.
            var cidr = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 64 : 32;
            addressRange = (targetAddress, cidr);
        }

        var cfg = UnsafePseudoIoC.ConfigurationManager;
        var serverName = cfg.GetCVar(CCVars.AdminLogsServerName);

        if (isGlobalBan)
        {
            serverName = "unknown";
        }

        var locatedPlayer = await _playerLocator.LookupIdByNameOrIdAsync(admin);
        if (locatedPlayer == null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = (await _db.GetPlayTimes(targetUid)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var player = locatedPlayer.UserId;
        var banDef = new ServerRoleBanDef(
            null,
            targetUid,
            addressRange,
            targetHWid,
            DateTimeOffset.Now,
            expires,
            roundId,
            playtime,
            reason,
            NoteSeverity.High,
            player,
            null,
            role,
            serverName);

        if (!await AddRoleBan(banDef))
        {
            UtkaSendResponse(false, context);
            return;
        }

        var banId = await UtkaGetBanId(reason, role, targetUid);

        UtkaSendJobBanEvent(admin, target, minutes, job, isGlobalBan, reason, banId);
        UtkaSendResponse(true, context);

        SendRoleBans(located);
    }

    private void UtkaSendResponse(bool banned, IPandaStatusHandlerContext context)
    {
        var utkaBanned = new UtkaJobBanResponse()
        {
            Banned = banned
        };

        context.RespondJsonAsync(utkaBanned);
    }

    private async void UtkaSendJobBanEvent(string ackey, string ckey, uint duration, string job, bool global,
        string reason, int banId)
    {
        if (job.Contains("Job:"))
        {
            job = job.Replace("Job:", "");
        }

        var utkaBanned = new UtkaBannedEvent()
        {
            ACkey = ackey,
            Ckey = ckey,
            Duration = duration,
            Bantype = job,
            Global = global,
            Reason = reason,
            Rid = EntitySystem.Get<GameTicker>().RoundId,
            BanId = banId
        };

        _pandaWeb.SendBotPostMessage(utkaBanned);
        _entMan.EventBus.RaiseEvent(EventSource.Local, utkaBanned);
    }

    private async Task<int> UtkaGetBanId(string reason, string role, NetUserId targetUid)
    {
        var banId = 0;
        var banList = await _db.GetServerRoleBansAsync(null, targetUid, null);

        foreach (var ban in banList)
        {
            if (ban.Reason == reason)
            {
                if (ban.Role == role && ban.Id != null)
                {
                    banId = ban.Id.Value;
                }
            }
        }

        return banId;
    }

    public void SendRoleBans(LocatedPlayerData located)
    {
        if (!_playerManager.TryGetSessionById(located.UserId, out var player))
        {
            return;
        }

        SendRoleBans(player);
    }
    //WD end
}
