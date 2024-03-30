using Content.Server.GameTicking.Rules;
using System.Linq;
using Content.Server._Miracle.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Station.Systems;
using Robust.Server.Player;
using Robust.Shared.Utility;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Shared.Points;
using Robust.Shared.Random;

namespace Content.Server._Miracle.GameRules;

// TODO: отредачить pointsystem и sharedpointsystem чтобы оно считало очки команд - сделано?
// TODO: дать экшен или предмет на выход из матча? - удалять человека из RespawnTrackerComponent.Players
// TODO: инициализация команд (просто добавить в прототип геймрула), функция для смены команды? вставить куда-нибудь EnsureTeam из PointSystem?
// TODO: прототипы геймрула, аплинка и startingGear, gameMapPool, сама карта
// TODO: мб дать в startingGear аплинк, который будет не просто давать предметы, но и сразу их надевать на космонавтика
// TODO: нормальный RoundEndTextAppend и TeamScoreboard

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
        SubscribeLocalEvent<ViolenceRuleComponent, PlayerPointChangedEvent>(OnPointChanged);
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
            if (!ruleComponent.TeamMembers.TryGetValue(ev.Player, out var team))
                ruleComponent.TeamMembers.Add(ev.Player, ruleComponent.Teams[_robustRandom.Next(0, ruleComponent.Teams.Count())]);

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


            // YOU SUICIDED OR GOT THROWN INTO LAVA!
            // WHAT A GIANT FUCKING NERD! LAUGH NOW!
            if (ev.Primary is not KillPlayerSource player)
            {
                _point.AdjustPointValue(ev.Entity, -1, uid, point);
                // -1 point to the nerd's team?
                continue;
            }


            // adjust team points here
            _point.AdjustPointValue(player.PlayerId, 1, uid, point);


            if (ev.Assist is KillPlayerSource assist && ruleComponent.Victor == null)
                _point.AdjustPointValue(assist.PlayerId, 0.5, uid, point);

            // I dont know if we will have reward spawns or any direct rewards for players
            //var spawns = EntitySpawnCollection.GetSpawns(ruleComponent.RewardSpawns).Cast<string?>().ToList();
            //EntityManager.SpawnEntities(Transform(ev.Entity).MapPosition, spawns);
        }
    }

    private void OnPointChanged(EntityUid uid, ViolenceRuleComponent component, ref PlayerPointChangedEvent args)
    {
        if (component.Victor != null)
            return;

        if (args.Points < component.PointCap)
            return;

        //component.Victor = args.Player; // should assign a team as victor probably?
        _roundEnd.EndRound(component.RestartDelay);
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
