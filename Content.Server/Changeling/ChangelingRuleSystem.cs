using System.Linq;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Changeling;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Changeling;

public sealed class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    private const int PlayersPerChangeling = 1; //default 10
    private const int MaxChangelings = 5;

    private const float ChangelingStartDelay = 3f * 60;
    private const float ChangelingStartDelayVariance = 3f * 60;

    private const int ChangelingMinPlayers = 0; //default 10

    private const int ChangelingMaxDifficulty = 5;
    private const int ChangelingMaxPicks = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);

        // SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
        // SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    protected override void ActiveTick(EntityUid uid, ChangelingRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus == ChangelingRuleComponent.SelectionState.ReadyToSelect && _gameTiming.CurTime > component.AnnounceAt)
            DoChangelingStart(component);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = ChangelingMinPlayers;
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("changeling-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("changeling-no-one-ready"));
                ev.Cancel();
            }
        }
    }
    private void DoChangelingStart(ChangelingRuleComponent component)
    {
        if (!component.StartCandidates.Any())
        {
            Log.Error("Tried to start Changeling mode without any candidates.");
            return;
        }

        var numChangelings = MathHelper.Clamp(component.StartCandidates.Count / PlayersPerChangeling, 1, MaxChangelings);
        var changelingPool = _antagSelection.FindPotentialAntags(component.StartCandidates, component.ChangelingPrototypeId);
        var selectedChangelings = _antagSelection.PickAntag(numChangelings, changelingPool);

        foreach (var changeling in selectedChangelings)
        {
            MakeChangeling(changeling);
        }

        component.SelectionStatus = ChangelingRuleComponent.SelectionState.SelectionMade;
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var changeling, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                changeling.StartCandidates[player] = ev.Profiles[player.UserId];
            }

            var delay = TimeSpan.FromSeconds(ChangelingStartDelay +
                                             _random.NextFloat(0f, ChangelingStartDelayVariance));

            changeling.AnnounceAt = _gameTiming.CurTime + delay;

            changeling.SelectionStatus = ChangelingRuleComponent.SelectionState.ReadyToSelect;
        }
    }

    public bool MakeChangeling(ICommonSession changeling, bool giveObjectives = true)
    {
        var changelingRule = EntityQuery<ChangelingRuleComponent>().FirstOrDefault();
        if (changelingRule == null)
        {
            GameTicker.StartGameRule("Changeling", out var ruleEntity);
            changelingRule = Comp<ChangelingRuleComponent>(ruleEntity);
        }

        if (!_mindSystem.TryGetMind(changeling, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked changeling.");
            return false;
        }

        if (HasComp<ChangelingRoleComponent>(mindId))
        {
            Log.Error($"Player {changeling.Name} is already a changeling.");
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Log.Error("Mind picked for changeling did not have an attached entity.");
            return false;
        }

        _roleSystem.MindAddRole(mindId, new ChangelingRoleComponent
        {
            PrototypeId = changelingRule.ChangelingPrototypeId
        }, mind);

        _roleSystem.MindPlaySound(mindId, changelingRule.GreetSoundNotification, mind);
        changelingRule.ChangelingMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(entity, "NanoTrasen", false);
        _npcFaction.AddFaction(entity, "Syndicate");

        EnsureComp<ChangelingComponent>(entity);

        // TODO Add objectives
        // if (giveObjectives)
        // {
        //     var maxDifficulty = ChangelingMaxDifficulty;
        //     var maxPicks = ChangelingMaxPicks;
        //     var difficulty = 0f;
        //     for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        //     {
        //         var objective = _objectives.GetRandomObjective(mindId, mind, "TraitorObjectiveGroups");
        //         if (objective == null)
        //             continue;
        //
        //         _mindSystem.AddObjective(mindId, mind, objective.Value);
        //         difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
        //     }
        // }

        return true;
    }

    /// <summary>
    ///     Send a codewords and uplink codes to traitor chat.
    /// </summary>
    /// <param name="mind">A mind (player)</param>
    /// <param name="codewords">Codewords</param>
    /// <param name="code">Uplink codes</param>
    // private void SendTraitorBriefing(EntityUid mind, string[] codewords, Note[]? code)
    // {
    //     if (!_mindSystem.TryGetSession(mind, out var session))
    //         return;
    //
    //     _chatManager.DispatchServerMessage(session, Loc.GetString("traitor-role-greeting"));
    //     _chatManager.DispatchServerMessage(session, Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", codewords))));
    //     if (code != null)
    //         _chatManager.DispatchServerMessage(session, Loc.GetString("traitor-role-uplink-code", ("code", string.Join("-", code).Replace("sharp","#"))));
    // }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var changeling, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (changeling.TotalChangelings >= MaxChangelings)
                continue;
            if (!ev.LateJoin)
                continue;
            if (!ev.Profile.AntagPreferences.Contains(changeling.ChangelingPrototypeId))
                continue;

            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                continue;

            if (!job.CanBeAntag)
                continue;

            // Before the announcement is made, late-joiners are considered the same as players who readied.
            if (changeling.SelectionStatus < ChangelingRuleComponent.SelectionState.SelectionMade)
            {
                changeling.StartCandidates[ev.Player] = ev.Profile;
                continue;
            }

            var target = PlayersPerChangeling * changeling.TotalChangelings + 1;

            var chance = 1f / PlayersPerChangeling;

            if (ev.JoinOrder < target)
            {
                chance /= (target - ev.JoinOrder);
            }
            else
            {
                chance *= ((ev.JoinOrder + 1) - target);
            }

            if (chance > 1)
                chance = 1;

            if (_random.Prob(chance))
            {
                MakeChangeling(ev.Player);
            }
        }
    }

    // private void OnObjectivesTextGetInfo(EntityUid uid, ChangelingRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    // {
    //     args.Minds = comp.ChangelingMinds;
    //     args.AgentName = Loc.GetString("traitor-round-end-agent-name");
    // }

    // private void OnObjectivesTextPrepend(EntityUid uid, ChangelingRuleComponent comp, ref ObjectivesTextPrependEvent args)
    // {
    //     args.Text += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", comp.Codewords)));
    // }

    // public List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind)
    // {
    //     List<(EntityUid Id, MindComponent Mind)> allTraitors = new();
    //     foreach (var traitor in EntityQuery<ChangelingRuleComponent>())
    //     {
    //         foreach (var role in GetOtherTraitorMindsAliveAndConnected(ourMind, traitor))
    //         {
    //             if (!allTraitors.Contains(role))
    //                 allTraitors.Add(role);
    //         }
    //     }
    //
    //     return allTraitors;
    // }
    //
    // private List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind, ChangelingRuleComponent component)
    // {
    //     var changelings = new List<(EntityUid Id, MindComponent Mind)>();
    //     foreach (var changeling in component.ChangelingMinds)
    //     {
    //         if (TryComp(changeling, out MindComponent? mind) &&
    //             mind.OwnedEntity != null &&
    //             mind.Session != null &&
    //             mind != ourMind &&
    //             _mobStateSystem.IsAlive(mind.OwnedEntity.Value) &&
    //             mind.CurrentEntity == mind.OwnedEntity)
    //         {
    //             changelings.Add((changeling, mind));
    //         }
    //     }
    //
    //     return changelings;
    // }
}
