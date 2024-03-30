using Content.Server.GameTicking.Rules;
using System.Linq;
using Content.Server._Miracle.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Robust.Shared.Utility;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Shared.Points;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Miracle.GameRules;

// TODO: edit pointsystem and sharedpointsystem so that it correctly manages team points - осталось только тестить
// TODO: give player a button to switch teams
// TODO: use EnsureTeam from PointSystem?
// TODO: prototypes of gamerule, uplink and startingGear, gameMapPool, the map itself
// TODO: maybe include and uplink in startingGear that wont only give items, but equip them instantly
// TODO: proper RoundEndTextAppend and TeamScoreboard

public sealed class ViolenceRuleSystem : GameRuleSystem<ViolenceRuleComponent>
{
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly RespawnRuleSystem _respawn = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        //SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ViolenceRuleComponent, TeamPointChangedEvent>(OnPointChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<ViolenceRuleComponent, RespawnTrackerComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var tracker, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mind.SetUserId(newMind, ev.Player.UserId);

            // Assign player to a team, if he has none
            if (!ruleComponent.TeamMembers.TryGetValue(ev.Player.UserId, out var team))
                ruleComponent.TeamMembers.Add(ev.Player.UserId, ruleComponent.Teams[_robustRandom.Next(0, ruleComponent.Teams.Count())]);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile, null, team); // make it spawn on specified team spawnpoints
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

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<KillTrackerComponent>(ev.Mob);
        var query = EntityQueryEnumerator<ViolenceRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            _respawn.AddToTracker(ev.Mob, uid, tracker);
        }
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<ViolenceRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var team = GetEntitiesTeam(ev.Entity, ruleComponent);
            // YOU SUICIDED OR GOT THROWN INTO LAVA!
            // WHAT A GIANT FUCKING NERD! LAUGH NOW!
            if (ev.Primary is not KillPlayerSource player)
            {
                _point.AdjustPointValue(ev.Entity, -1, uid, point); // krill issue penalty
                // -1 point to the nerd's team?
                continue;
            }

            if (ev.Primary is KillPlayerSource killer && ruleComponent.TeamMembers.TryGetValue(killer.PlayerId, out var suckersTeam))
            {
                if (team != suckersTeam && team != null)
                {
                    _point.AdjustTeamPointValue(team.Value, 1, uid, point);
                    _point.AdjustPointValue(killer.PlayerId, 1, uid, point);
                }
            }


            if (ev.Assist is KillPlayerSource assist && ruleComponent.Victor == null && ruleComponent.TeamMembers.TryGetValue(assist.PlayerId, out var shitTeam))
            {
                if (team != shitTeam && team != null)
                {
                    _point.AdjustTeamPointValue(team.Value, 0.5, uid, point);
                    _point.AdjustPointValue(assist.PlayerId, 0.5, uid, point);
                }

            }

            // I dont know if we will have reward spawns or any direct rewards for players
            //var spawns = EntitySpawnCollection.GetSpawns(ruleComponent.RewardSpawns).Cast<string?>().ToList();
            //EntityManager.SpawnEntities(Transform(ev.Entity).MapPosition, spawns);
        }
    }

    private void OnPointChanged(EntityUid uid, ViolenceRuleComponent component, ref TeamPointChangedEvent args)
    {
        if (component.Victor != null)
            return;

        if (args.Points < component.PointCap)
            return;

        component.Victor = args.Team;
        _roundEnd.EndRound(component.RestartDelay);
    }

    private ushort? GetEntitiesTeam(EntityUid uid, ViolenceRuleComponent comp)
    {
        ActorComponent? actor = null;
        if (!Resolve(uid, ref actor, false))
            return null;

        if (!comp.TeamMembers.TryGetValue(actor.PlayerSession.UserId, out var team))
            return null;

        return team;
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

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<ViolenceRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComponent, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, rule))
                continue;

            /*
            if (ruleComponent.Victor != null && _player.TryGetPlayerData(ruleComponent.Victor.Value, out var data)) // get the team data here
            {
                ev.AddLine(Loc.GetString("point-scoreboard-winner", ("player", data.UserName)));
                ev.AddLine("");
            }
            */
            ev.AddLine(Loc.GetString("point-scoreboard-header")); // edit this, probably
            ev.AddLine(new FormattedMessage(point.Scoreboard).ToMarkup());
        }
    }
}
