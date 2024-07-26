using Content.Server.Antag;
using Content.Server.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared._White.Mood;
using Content.Shared.Changeling;
using Content.Shared.GameTicking;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;

namespace Content.Server.Changeling;

public sealed class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly ChangelingNameGenerator _nameGenerator = default!;

    private const int PlayersPerChangeling = 15;
    private const int MaxChangelings = 4;

    private const int ChangelingMaxDifficulty = 5;
    private const int ChangelingMaxPicks = 20;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(ClearUsedNames);

        SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

        SubscribeLocalEvent<ChangelingRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    protected override void Added(EntityUid uid, ChangelingRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        gameRule.MinPlayers = PlayersPerChangeling;
    }

    private void OnGetBriefing(Entity<ChangelingRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("changeling-role-briefing-short"));
    }

    private void AfterEntitySelected(Entity<ChangelingRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeChangeling(args.EntityUid, ent);
    }

    private void ClearUsedNames(RoundRestartCleanupEvent ev)
    {
        _nameGenerator.ClearUsed();
    }

    private void OnObjectivesTextGetInfo(
        EntityUid uid,
        ChangelingRuleComponent comp,
        ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.ChangelingMinds;
        args.AgentName = Loc.GetString("changeling-round-end-agent-name");
    }

    public bool MakeChangeling(EntityUid changeling, ChangelingRuleComponent rule, bool giveObjectives = true)
    {
        if (!_mindSystem.TryGetMind(changeling, out var mindId, out var mind))
        {
            return false;
        }

        if (HasComp<ChangelingRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is already a changeling.");
            return false;
        }

        var briefing = Loc.GetString("changeling-role-greeting");
        _antagSelection.SendBriefing(changeling, briefing, null, rule.GreetSoundNotification);

        rule.ChangelingMinds.Add(mindId);

        _roleSystem.MindAddRole(mindId, new ChangelingRoleComponent
        {
            PrototypeId = rule.ChangelingPrototypeId
        }, mind);

        // Change the faction
        _npcFaction.RemoveFaction(changeling, "NanoTrasen", false);
        _npcFaction.AddFaction(changeling, "Changeling");

        EnsureComp<ChangelingComponent>(changeling, out var readyChangeling);

        readyChangeling.HiveName = _nameGenerator.GetName();
        Dirty(changeling, readyChangeling);

        RaiseLocalEvent(changeling, new MoodEffectEvent("TraitorFocused"));

        if (!giveObjectives)
            return true;

        var difficulty = 0f;
        for (var pick = 0; pick < ChangelingMaxPicks && ChangelingMaxDifficulty > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "ChangelingObjectiveGroups");
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            var adding = Comp<ObjectiveComponent>(objective.Value).Difficulty;
            difficulty += adding;
            Log.Debug($"Added objective {ToPrettyString(objective):objective} with {adding} difficulty");
        }

        return true;
    }
}
