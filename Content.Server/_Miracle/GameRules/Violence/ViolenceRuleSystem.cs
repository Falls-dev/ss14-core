using Content.Server.GameTicking.Rules;
using System.Linq;
using Content.Server._Miracle.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Inventory;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Robust.Shared.Utility;
using Content.Server.KillTracking;
using Content.Server.Maps;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.Polymorph.Systems;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Points;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Miracle.GameRules;

// TODO: respawns may suck
// TODO: recheck matchflow and roundflow
// TODO: test buying equipment
// TODO: prototypes of gamerule, uplink and startingGear, gameMapPool, the map itself
// TODO: make a menu to join the round, switch teams, leave the round, get scoreboard - мб сделает валтос

public sealed class ViolenceRuleSystem : GameRuleSystem<ViolenceRuleComponent>
{
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly RespawnRuleSystem _respawn = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;

    private ISawmill _sawmill = default!;

    private List<EntityUid> _activeRules = new List<EntityUid>();

    private readonly string _defaultViolenceRule = "Violence";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("violence");
        //_sawmillReplays = _logManager.GetSawmill("violence.replays");

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        //SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ViolenceRuleComponent, TeamPointChangedEvent>(OnPointChanged);
        //SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    protected override void ActiveTick(EntityUid uid, ViolenceRuleComponent comp, GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, comp, gameRule, frameTime);

        switch (comp.RoundState)
        {
            case RoundState.InProgress:
                if (_timing.CurTime >= comp.RoundEndTime)
                {
                    EndRound(comp);
                }
                break;

            case RoundState.Starting:

                break;

            case RoundState.NotInProgress:
                if (_timing.CurTime >= comp.RoundStartTime)
                {
                    StartRound(uid, comp);
                }
                break;
        }
    }

    /// <summary>
    /// This method handles roundstart player spawning. After roundstart, players will be spawned by RespawnRuleSystem.
    /// </summary>
    /// <param name="ev"></param>
    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        /*
        if (ev.LateJoin) // this will allow this gamerule to be added to usual rounds. maybe
            return;
        */
        var query = EntityQueryEnumerator<ViolenceRuleComponent, RespawnTrackerComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var tracker, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var teamList = TeamsEligibleToJoin(ruleComponent);
            if (teamList.Count == 0)
            {
                continue;
            }

            if (!ruleComponent.TeamMembers.TryGetValue(ev.Player.UserId, out var team))
            {
                ruleComponent.TeamMembers.Add(ev.Player.UserId, teamList[_robustRandom.Next(0, teamList.Count())]);
            }

            var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mind.SetUserId(newMind, ev.Player.UserId);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile, null, team);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);
            // Should get startingGear from the client here. Setting default startingGear if none or invalid is passed.
            // Also different startingGear for different teams. A dictionary of teams and startingGear in ViolenceRuleComponent?
            //SetOutfitCommand.SetOutfit(mob, ruleComponent.Gear, EntityManager);
            EnsureComp<KillTrackerComponent>(mob);
            _respawn.AddToTracker(ev.Player.UserId, uid, tracker);

            _point.EnsurePlayer(ev.Player.UserId, uid, point);

            ev.Handled = true;
            break;
        }
    }

    /// <summary>
    /// TODO: think about it. i think it is not needed. respawns are handled by StartRound anyway
    /// </summary>
    /// <param name="ev"></param>
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<KillTrackerComponent>(ev.Mob);
        var query = EntityQueryEnumerator<ViolenceRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var violence, out var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (violence.TeamMembers.ContainsKey(ev.Player.UserId))
                _respawn.AddToTracker(ev.Mob, uid, tracker);

            if (violence.SavedEquip.TryGetValue(ev.Player.UserId, out var equip))
            {
                foreach (var equipId in equip)
                {
                    if (violence.EquipSlots.TryGetValue(equipId, out var slot))
                    {
                        _transform.SetParent(equipId, ev.Station);
                        // TODO: teleport equipId right under the player
                        if (slot == "hand")
                        {
                            _hands.PickupOrDrop(ev.Mob, equipId);
                        }
                        else
                        {
                            _inventory.TryEquip(ev.Mob, equipId, slot);
                        }
                    }
                }
                equip.Clear();
            }
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        if (!TryComp<ActorComponent>(ev.Target, out var actor))
            return;

        var query = EntityQueryEnumerator<ViolenceRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var team = GetPlayersTeam(actor.PlayerSession.UserId, ruleComponent);
            if (team == null)
                continue;

            var alive = GetAliveTeamMembersCount(team.Value, ruleComponent);
            if (alive == 0)
            {
                if (CheckForRoundEnd(ruleComponent))
                    DoRoundEndBehavior(ruleComponent);
            }
        }
    }

    public bool StartRound(EntityUid uid, ViolenceRuleComponent comp)
    {
        if (comp.RoundState != RoundState.NotInProgress)
        {
            // probably assert here?
            return false;
        }

        if (!comp.CurrentMap.HasValue)
        {
            comp.CurrentMap = _mapManager.CreateMap();

            if (!comp.CurrentMap.HasValue)
                return false; // i give up. or maybe load default map?

            _mapManager.AddUninitializedMap(comp.CurrentMap.Value);

            _prototypeManager.TryIndex<GameMapPoolPrototype>(comp.MapPool, out var extractedMapPool);
            if (extractedMapPool == null)
                return false;

            var mapPrototype = extractedMapPool.Maps.ElementAt(_robustRandom.Next(extractedMapPool.Maps.Count));
            _prototypeManager.TryIndex<GameMapPrototype>(mapPrototype, out var map);

            if (map != null)
            {
                _gameTicker.LoadGameMap(map, comp.CurrentMap.Value, null);
            }
            else
            {
                return false;
            }
        }

        // TODO: recheck the rest of this method
        // spawning players
        foreach (var (playerId, teamId) in comp.TeamMembers)
        {
            var player = _playerManager.GetSessionById(playerId);
            if (player == null)
                continue;

            var profile = _gameTicker.GetPlayerProfile(player);

            var newMind = _mind.CreateMind(playerId, profile.Name);
            _mind.SetUserId(newMind, playerId);

            if (!comp.CurrentMap.HasValue)
                return false;
            var station = _mapManager.GetMapEntityId(comp.CurrentMap.Value);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(station, null, profile, null, teamId);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);
            // Should get startingGear from the client here. Setting default startingGear if none or invalid is passed.
            // Also different startingGear for different teams. A dictionary of teams and startingGear in ViolenceRuleComponent?
            //SetOutfitCommand.SetOutfit(mob, ruleComponent.Gear, EntityManager);
            EnsureComp<KillTrackerComponent>(mob);

            if (TryComp<RespawnTrackerComponent>(uid, out var tracker))
            {
                _respawn.AddToTracker(playerId, uid, tracker);
            }
            else
            {
                _respawn.AddToTracker(playerId, uid);
            }

            _point.EnsurePlayer(playerId, uid);

            DebugTools.AssertNotNull(mobMaybe);
        }

        // Set round state and end time
        comp.RoundState = RoundState.InProgress;
        comp.RoundEndTime = _timing.CurTime + comp.RoundDuration;

        return true;
    }

    public bool DoRoundEndBehavior(ViolenceRuleComponent comp)
    {
        // announce the winner
        foreach (var (player, team) in comp.TeamMembers)
        {
            if (team != comp.MatchVictor)
                continue;

            if (!_playerManager.SessionsDict.TryGetValue(player, out var session))
                continue;


            _polymorphSystem.EnsurePausedMap();

            if (session.AttachedEntity != null &&
                TryComp<MobStateComponent>(session.AttachedEntity, out var mobState) &&
                mobState.CurrentState == MobState.Alive)
            {
                if (_polymorphSystem.PausedMap != null)
                {
                    if (!comp.SavedEquip.ContainsKey(session.UserId))
                    {
                        comp.SavedEquip.Add(session.UserId, new List<EntityUid>());
                    }

                    if (TryComp<HandsComponent>(session.AttachedEntity, out var hands))
                    {
                        foreach (var hand in _hands.EnumerateHeld(session.AttachedEntity.Value))
                        {
                            _hands.TryDrop(session.AttachedEntity.Value, hand, checkActionBlocker: false);
                            _transform.SetParent(hand, Transform(hand), _polymorphSystem.PausedMap.Value);
                            comp.SavedEquip[session.UserId].Add(hand);
                            comp.EquipSlots.Add(hand, "hand");
                        }
                    }
                    if (TryComp<InventoryComponent>(session.AttachedEntity, out var inv))
                    {
                        var enumerator = new InventorySystem.InventorySlotEnumerator(inv);
                        while (enumerator.NextItem(out var item, out var slot))
                        {
                            _inventory.TryUnequip(session.AttachedEntity.Value, slot.Name, true, true);
                            _transform.SetParent(item, Transform(item), _polymorphSystem.PausedMap.Value);
                            comp.SavedEquip[session.UserId].Add(item);
                            comp.EquipSlots.Add(item, slot.Name);
                        }
                    }
                }

                // ummm TODO: test this
                if (comp.Money.ContainsKey(session.UserId))
                {
                    comp.Money[session.UserId] += comp.AliveReward;
                }
                else
                {
                    comp.Money.TryAdd(session.UserId, comp.AliveReward);
                }
            }



        }
        // wait for comp.RoundEndDelay
        return EndRound(comp);
    }

    public bool EndRound(ViolenceRuleComponent comp)
    {
        if (comp.RoundState == RoundState.NotInProgress)
        {
            // probably assert here?
            return false;
        }

        comp.RoundState = RoundState.NotInProgress;

        if (comp.CurrentMap.HasValue)
            _mapManager.DeleteMap(comp.CurrentMap.Value);

        comp.RoundStartTime = _timing.CurTime + comp.RoundStartDelay;
        return true;
    }


    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<ViolenceRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            ActorComponent? actor = null;
            if (!Resolve(uid, ref actor, false))
                return;

            var team = GetPlayersTeam(actor.PlayerSession.UserId, ruleComponent);
            if (team == null)
                return;

            if (!ruleComponent.KillDeaths.TryGetValue(actor.PlayerSession.UserId, out var targetKd))
            {
                targetKd = new List<int>();
                targetKd.Add(0);
                targetKd.Add(0);
                targetKd.Add(0);

                ruleComponent.KillDeaths.Add(actor.PlayerSession.UserId, targetKd);
            }

            targetKd[2]++;

            // YOU SUICIDED OR GOT THROWN INTO LAVA!
            // WHAT A GIANT FUCKING NERD! LAUGH NOW!
            if (ev.Primary is not KillPlayerSource player)
            {
                _point.AdjustPointValue(ev.Entity, -1, uid, point); // krill issue penalty
                // -1 point to the nerd's team?
                continue;
            }

            if (ev.Primary is KillPlayerSource killer && ruleComponent.TeamMembers.TryGetValue(killer.PlayerId, out var killerTeam))
            {
                if (team != killerTeam)
                {
                    if (!ruleComponent.KillDeaths.TryGetValue(killer.PlayerId, out var killerKd))
                    {
                        killerKd = new List<int>();
                        killerKd.Add(0);
                        killerKd.Add(0);
                        killerKd.Add(0);

                        ruleComponent.KillDeaths.Add(actor.PlayerSession.UserId, killerKd);
                    }

                    killerKd[0]++;
                }
            }


            if (ev.Assist is KillPlayerSource assist && ruleComponent.MatchVictor == null && ruleComponent.TeamMembers.TryGetValue(assist.PlayerId, out var assistTeam))
            {
                if (team != assistTeam)
                {
                    if (!ruleComponent.KillDeaths.TryGetValue(assist.PlayerId, out var assistKd))
                    {
                        assistKd = new List<int>();
                        assistKd.Add(0);
                        assistKd.Add(0);
                        assistKd.Add(0);

                        ruleComponent.KillDeaths.Add(actor.PlayerSession.UserId, assistKd);
                    }

                    assistKd[1]++;
                }
            }

            // I dont know if we will have reward spawns or any direct rewards for players
            //var spawns = EntitySpawnCollection.GetSpawns(ruleComponent.RewardSpawns).Cast<string?>().ToList();
            //EntityManager.SpawnEntities(Transform(ev.Entity).MapPosition, spawns);
        }
    }

    private void OnPointChanged(EntityUid uid, ViolenceRuleComponent component, ref TeamPointChangedEvent args)
    {
        if (component.MatchVictor != null)
            return;

        if (args.Points <= component.PointCap)
            return;

        DoRoundEndBehavior(component);

        component.MatchVictor = args.Team;
        _gameTicker.EndGameRule(uid);
        if (ActiveMatches() == 0)
        {
            // maybe wait a bit before ending the round?
            _roundEnd.DoRoundEndBehavior(RoundEndBehavior.InstantEnd, TimeSpan.FromSeconds(10));
        }
        // DoRoundEndBehavior(uid);???
    }

    public bool TryBuyItem(NetUserId player, ViolenceRuleComponent comp, string item)
    {
        if (!comp.TeamMembers.TryGetValue(player, out var team))
            return false;

        if (!comp.Shop.TryGetValue(team, out var shop))
            return false;

        if (!shop.TryGetValue(item, out var listing))
            return false;

        if (!comp.Money.TryGetValue(player, out var money))
            return false;


        if (money >= listing.Price)
        {
            if (!_prototypeManager.TryIndex(item, out var proto))
                return false;

            comp.Money[player] -= listing.Price;

            if (!_playerManager.GetSessionById(player).AttachedEntity.HasValue)
            {

            }

            _polymorphSystem.EnsurePausedMap();
            var equip = EntityManager.SpawnEntity(item, new EntityCoordinates(_polymorphSystem.PausedMap!.Value, 0, 0));

            if (!comp.SavedEquip.ContainsKey(player))
            {
                comp.SavedEquip.Add(player, new List<EntityUid>());
            }
            comp.SavedEquip[player].Add(equip);
            comp.EquipSlots.Add(equip, listing.Slot);
        }

        return false;
    }

    private int ActiveMatches()
    {
        int count = 0;
        var query = EntityQueryEnumerator<ViolenceRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var violence, out var rule))
        {
            if (GameTicker.IsGameRuleActive(uid, rule))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Tries to join the round
    /// </summary>
    /// <param name="player"></param>
    /// <param name="comp"></param>
    /// <param name="preferredTeam"></param>
    /// <param name="anyTeam"></param>
    /// <returns></returns>
    public bool JoinRound(NetUserId player, ViolenceRuleComponent comp, ushort? preferredTeam = null, bool anyTeam = false)
    {
        // Check if the player is already a member of a team
        if (comp.TeamMembers.ContainsKey(player))
        {
            return false;
        }

        var eligibleTeams = TeamsEligibleToJoin(comp);
        // If there are no eligible teams, return false
        if (eligibleTeams.Count == 0)
        {
            return false;
        }

        // If a team is specified, check if it's eligible for joining
        if (preferredTeam.HasValue)
        {
            if (eligibleTeams.Contains(preferredTeam.Value))
            {
                comp.TeamMembers.Add(player, preferredTeam.Value);
                return true;
            }

            if (!anyTeam)
                return false;
        }

        // Randomly select a team from the eligible teams and add the player to it
        var selectedTeam = eligibleTeams[_robustRandom.Next(0, eligibleTeams.Count)];
        comp.TeamMembers.Add(player, selectedTeam);

        return true;
    }

    public bool LeaveRound(NetUserId player, ViolenceRuleComponent comp)
    {
        if (!comp.TeamMembers.ContainsKey(player))
            return false;

        comp.TeamMembers.Remove(player);
        return true;
    }

    /// <summary>
    /// Get a player's team from EntityUid and ViolenceRuleComponent
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <returns></returns>
    private ushort? GetPlayersTeam(NetUserId userId, ViolenceRuleComponent comp)
    {
        if (!comp.TeamMembers.TryGetValue(userId, out var team))
            return null;

        return team;
    }

    public int GetAliveTeamMembersCount(ushort teamId, ViolenceRuleComponent comp)
    {
        var alive = 0;

        foreach (var (player, team) in comp.TeamMembers)
        {
            if (team == teamId)
            {
                var mind = _mind.GetMind(player);
                if (mind == null || TryComp<MindComponent>(mind, out var mindComponent) || mindComponent == null)
                    continue;

                if (mindComponent.OwnedEntity == null)
                    continue;

                if (!TryComp<MobStateComponent>(mindComponent.OwnedEntity.Value, out var mobStateComponent))
                    continue;

                if (mobStateComponent.CurrentState != MobState.Alive)
                    continue;

                alive++;
            }
        }

        return alive;
    }

    /// <summary>
    /// Round ends only if players of one single team are alive.
    /// </summary>
    /// <param name="comp"></param>
    /// <returns></returns>
    public bool CheckForRoundEnd(ViolenceRuleComponent comp)
    {
        var aliveTeams = new List<ushort>();

        foreach (var team in comp.Teams)
        {
            if (GetAliveTeamMembersCount(team, comp) > 0)
            {
                aliveTeams.Add(team);
            }
        }

        if (aliveTeams.Count == 1)
        {
            comp.MatchVictor = aliveTeams[0];
            return true;
        }

        if (aliveTeams.Count == 0)
        {
            return true;
        }

        return false;
    }

    // Do we even need that?
    public void SwitchTeam(NetUserId playerId, ushort newTeamId)
    {
        var query = EntityQueryEnumerator<ViolenceRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (!ruleComponent.Teams.Contains(newTeamId))
                continue;

            if (ruleComponent.TeamMembers[playerId] == newTeamId)
                return;

            var currentTeamId = ruleComponent.TeamMembers[playerId];
            var currentTeamCount = ruleComponent.TeamMembers.Values.Count(teamId => teamId == currentTeamId);
            var newTeamCount = ruleComponent.TeamMembers.Values.Count(teamId => teamId == newTeamId);

            if (newTeamCount >= currentTeamCount)
                return;

            ruleComponent.TeamMembers[playerId] = newTeamId;
        }
    }

    /// <summary>
    /// Create new instance of the rule entity. TODO: limit the number of these
    /// </summary>
    /// <param name="ruleId"></param>
    /// <returns></returns>
    public ViolenceRuleComponent? CreateNewInstance(string? ruleId)
    {
        if (ruleId == null)
            ruleId = _defaultViolenceRule;

        var ruleEntity = _gameTicker.AddGameRule(ruleId);

        if (!TryComp<ViolenceRuleComponent>(ruleEntity, out var comp))
        {
            EntityManager.DeleteEntity(ruleEntity);
            return null;
        }

        _gameTicker.StartGameRule(ruleEntity);
        _activeRules.Add(ruleEntity);

        return comp;
    }

    /// <summary>
    /// Delete the instance of the rule entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <returns>true if was deleted successfully</returns>
    public bool DeleteInstance(EntityUid uid)
    {
        if (!TryComp<ViolenceRuleComponent>(uid, out var comp))
        {
            return false;
        }

        comp.RoundState = RoundState.NotInProgress;

        // TODO: maybe delete all players from the round if deleting the map fucks that up?

        if (comp.CurrentMap.HasValue)
            _mapManager.DeleteMap(comp.CurrentMap.Value);

        _gameTicker.EndGameRule(uid);
        _activeRules.Remove(uid);

        return true;
    }



    public List<ushort> TeamsEligibleToJoin(ViolenceRuleComponent comp)
    {
        var teamList = new List<ushort>();

        foreach (var team in comp.Teams)
        {
            var teamCount = comp.TeamMembers.Values.Count(teamId => teamId == team);

            if (comp.MaxPlayers.TryGetValue(team, out var maxPlayers))
            {
                if (teamCount < maxPlayers)
                {
                    teamList.Add(team);
                }
            }
        }

        return teamList;
    }

    /*
    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<ViolenceRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, rule))
                continue;


            if (ruleComponent.MatchVictor != null && _player.TryGetPlayerData(ruleComponent.MatchVictor.Value, out var data)) // get the team data here
            {
                ev.AddLine(Loc.GetString("point-scoreboard-winner", ("player", data.UserName)));
                ev.AddLine("");
            }

            ev.AddLine(Loc.GetString("point-scoreboard-header")); // edit this, probably
            ev.AddLine(new FormattedMessage(point.Scoreboard).ToMarkup());
        }
    }
    */
}
