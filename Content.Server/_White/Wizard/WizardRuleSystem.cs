using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.NPC.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.GameTicking.Components;
using Content.Server.Objectives;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared._White.Antag;
using Content.Shared.Dataset;
using Content.Shared.Mind;
using Content.Shared.NPC.Components;
using Content.Shared.Objectives.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Random;

namespace Content.Server._White.Wizard;

/// <summary>
/// This handles...
/// </summary>
public sealed class WizardRuleSystem : GameRuleSystem<WizardRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WizardComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WizardRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
        //SubscribeLocalEvent<WizardRuleComponent, AntagSelectLocationEvent>(OnObjectivesTextGetInfo);
    }

    protected override void Added(EntityUid uid, WizardRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        gameRule.MinPlayers = component.MinPlayers;
    }

    private void OnObjectivesTextGetInfo(Entity<WizardRuleComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = ent.Comp.WizardMinds;
        args.AgentName = Loc.GetString("wizard-round-end-agent-name");
    }

    private void OnMobStateChanged(EntityUid uid, WizardComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead && component.EndRoundOnDeath)
            CheckAnnouncement();
    }

    private bool AddRole(EntityUid mindId, MindComponent mind, WizardRuleComponent wizardRule)
    {
        if (_roles.MindHasRole<WizardRoleComponent>(mindId))
            return false;

        wizardRule.WizardMinds.Add(mindId);

        var role = wizardRule.WizardRoleProto;
        _roles.MindAddRole(mindId, new WizardRoleComponent {PrototypeId = role});

        GiveObjectives(mindId, mind, wizardRule);

        return true;
    }

    private void GiveObjectives(EntityUid mindId, MindComponent mind, WizardRuleComponent wizardRule)
    {
        _mind.TryAddObjective(mindId, mind, "WizardSurviveObjective");

        var difficulty = 0f;
        for (var pick = 0; pick < 6 && 8 > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, wizardRule.ObjectiveGroup);
            if (objective == null)
                continue;

            _mind.AddObjective(mindId, mind, objective.Value);
            var adding = Comp<ObjectiveComponent>(objective.Value).Difficulty;
            difficulty += adding;
        }
    }

    private void CheckAnnouncement()
    {
        // Check for all at once gamemode
        if (GameTicker.GetActiveGameRules().Where(HasComp<RampingStationEventSchedulerComponent>).Any())
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var wizard, out _))
        {
            _roundEndSystem.DoRoundEndBehavior(
                wizard.RoundEndBehavior, wizard.EvacShuttleTime, wizard.RoundEndTextSender,
                wizard.RoundEndTextShuttleCall, wizard.RoundEndTextAnnouncement);

            return;
        }
    }

    private void SetupWizardEntity(
        EntityUid mob,
        StartingGearPrototype gear,
        bool endRoundOnDeath,
        bool randomPtofile = true)
    {
        EnsureComp<WizardComponent>(mob, out var component);
        component.EndRoundOnDeath = endRoundOnDeath;
        EnsureComp<GlobalAntagonistComponent>(mob).AntagonistPrototype = "globalAntagonistWizard";

        if (randomPtofile)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var profile = HumanoidCharacterProfile.RandomWithSpecies()
                .WithAge(random.Next(component.MinAge, component.MaxAge));

            var color = Color.FromHex(GetRandom(component.Color, "#B5B8B1"));
            var hair = GetRandom(component.Hair, "HumanHairAfricanPigtails");
            var facialHair = GetRandom(component.FacialHair, "HumanFacialHairAbe");
            profile = profile.WithCharacterAppearance(
                profile.WithCharacterAppearance(
                        profile.WithCharacterAppearance(
                                profile.WithCharacterAppearance(
                                        profile.Appearance.WithHairStyleName(hair))
                                    .Appearance.WithFacialHairStyleName(facialHair))
                            .Appearance.WithHairColor(color))
                    .Appearance.WithFacialHairColor(color));

            _humanoid.LoadProfile(mob, profile);

            _metaData.SetEntityName(mob, GetRandom(component.Name, ""));

            _stationSpawning.EquipStartingGear(mob, gear);
        }

        _npcFaction.RemoveFaction(mob, "NanoTrasen", false);
        _npcFaction.AddFaction(mob, "Wizard");
    }

    private EntityCoordinates WizardSpawnPoint(WizardRuleComponent component)
    {
        if (component.ShuttleMap is not {Valid: true} mapUid)
            return EntityCoordinates.Invalid;

        var spawn = new EntityCoordinates();
        foreach (var (_, meta, xform) in EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != component.SpawnPointProto.Id)
                continue;

            if (xform.MapUid != component.ShuttleMap)
                continue;

            spawn = xform.Coordinates;
            break;
        }

        // Fallback, spawn at the centre of the map
        if (spawn == new EntityCoordinates())
            spawn = Transform(mapUid).Coordinates;

        return spawn;
    }

    private void SpawnWizard(ICommonSession? session, WizardRuleComponent component, bool spawnGhostRoles = true)
    {
        var spawn = WizardSpawnPoint(component);
        if (spawn == EntityCoordinates.Invalid)
            return;

        var wizardAntag = _prototypeManager.Index(component.WizardRoleProto);

        //If a session is available, spawn mob and transfer mind into it
        if (session != null)
        {
            if (!_prototypeManager.TryIndex(SharedHumanoidAppearanceSystem.DefaultSpecies, out SpeciesPrototype? species))
            {
                species = _prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies);
            }

            var mob = Spawn(species.Prototype, spawn);
            if (!_prototypeManager.TryIndex(component.StartingGear, out var gear))
                return;

            SetupWizardEntity(mob, gear, true);
            var name = !TryComp<MetaDataComponent>(mob, out var meta) || meta.EntityName == "" ? "" : meta.EntityName;

            var newMind = _mind.CreateMind(session.UserId, name);
            _mind.SetUserId(newMind, session.UserId);
            AddRole(newMind.Owner, newMind.Comp, component);

            _mind.TransferTo(newMind, mob);
        }
        //Otherwise, spawn as a ghost role
        else if (spawnGhostRoles)
        {
            var spawnPoint = Spawn(component.GhostSpawnPointProto, spawn);
            var ghostRole = EnsureComp<GhostRoleComponent>(spawnPoint);
            EnsureComp<GhostRoleMobSpawnerComponent>(spawnPoint);
            ghostRole.RoleName = Loc.GetString(wizardAntag.Name);
            ghostRole.RoleDescription = Loc.GetString(wizardAntag.Objective);

            var wizardSpawner = EnsureComp<WizardSpawnerComponent>(spawnPoint);
            //TODO: maybe other params
        }
    }

    private string GetRandom(string list, string ifNull)
    {
        return _prototypeManager.TryIndex<DatasetPrototype>(list, out var prototype)
            ? _random.Pick(prototype.Values)
            : ifNull;
    }
}
