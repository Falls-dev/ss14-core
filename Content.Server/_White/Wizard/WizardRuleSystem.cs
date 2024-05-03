using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CCVar;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Nuke;
using Content.Server.NukeOps;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.RandomMetadata;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nuke;
using Content.Shared.NukeOps;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Tag;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Server.Maps;

namespace Content.Server._White.Wizard;

/// <summary>
/// This handles...
/// </summary>
public sealed class WizardRuleSystem : GameRuleSystem<WizardRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RandomMetadataSystem _randomMetadata = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;

    private ISawmill _sawmill = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);

        _sawmill = _logManager.GetSawmill("NukeOps");
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var wizardRule, out _))
        {
            if (!SpawnMap((uid, wizardRule)))
            {
                _sawmill.Info("Failed to load shuttle for wizard");
                continue;
            }

            //Handle there being nobody readied up
            if (ev.PlayerPool.Count == 0)
                continue;

            var wizardEligible =
                _antagSelection.GetEligibleSessions(ev.PlayerPool, wizardRule.WizardRoleProto);

            //Select wizard
            //Select Commander, priority : commanderEligible, agentEligible, operativeEligible, all players
            var selectedWizard = _antagSelection
                .ChooseAntags(1, wizardEligible, ev.PlayerPool).FirstOrDefault();

            SpawnWizard(selectedWizard,  wizardRule);

            if (selectedWizard != null)
                GameTicker.PlayerJoinGame(selectedWizard);
        }
    }

    protected override void Started(
        EntityUid uid,
        WizardRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (GameTicker.RunLevel == GameRunLevel.InRound)
            SpawnWizardGhostRole(uid, component);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        //TODO: wizard objectives and ftl

        ev.AddLine(Loc.GetString("nukeops-list-start"));

        var wizardsQuery = EntityQueryEnumerator<WizardRoleComponent, MindContainerComponent>();
        while (wizardsQuery.MoveNext(out var wizardUid, out _, out var mindContainer))
        {
            if (!_mind.TryGetMind(wizardUid, out _, out var mind, mindContainer))
                continue;

            ev.AddLine(mind.Session != null
                ? Loc.GetString("nukeops-list-name-user", ("name", Name(wizardUid)), ("user", mind.Session.Name))
                : Loc.GetString("nukeops-list-name", ("name", Name(wizardUid))));
        }
    }

    private void OnComponentRemove(EntityUid uid, WizardComponent component, ComponentRemove args)
    {
        CheckAnnouncement(component);
    }

    private void OnMobStateChanged(EntityUid uid, WizardComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            CheckAnnouncement(component);
    }

    private void OnPlayersGhostSpawning(EntityUid uid, WizardComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var spawner = args.Spawner;

        if (!TryComp<WizardSpawnerComponent>(spawner, out var wizardSpawner))
            return;

        HumanoidCharacterProfile? profile = null;
        if (TryComp(args.Spawned, out ActorComponent? actor))
            profile = _prefs.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter as HumanoidCharacterProfile;

        foreach (var wizardRule in EntityQuery<WizardRuleComponent>())
        {
            _prototypeManager.TryIndex(wizardSpawner.StartingGear, out var gear);
            SetupWizardEntity(uid, wizardSpawner.Name, wizardSpawner.Points, gear, profile);

        }
    }

    private void OnMindAdded(EntityUid uid, WizardComponent component, MindAddedMessage args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var wizardRule, out _))
        {
            var role = wizardRule.WizardRoleProto;
            _roles.MindAddRole(mindId, new WizardRoleComponent { PrototypeId = role });

            if (mind.Session is not { } playerSession)
                return;

            if (GameTicker.RunLevel != GameRunLevel.InRound)
                return;


            NotifyWizard(playerSession, component, wizardRule);

        }
    }

    private void OnRoundStart(EntityUid uid, WizardRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var filter = Filter.Empty();
        var query = EntityQueryEnumerator<WizardComponent, ActorComponent>();
        while (query.MoveNext(out _, out var wizard, out var actor))
        {
            NotifyWizard(actor.PlayerSession, wizard, component);
            filter.AddPlayer(actor.PlayerSession);
        }
    }

    private void CheckAnnouncement(WizardComponent comp)
    {
        if (comp.AnnouncementOnWizardDeath)
            //_announcementSystem.Announce(smt)
            return;
    }

    private bool SpawnMap(Entity<WizardRuleComponent> ent)
    {
        if (!ent.Comp.SpawnShuttle
            || ent.Comp.ShuttleMap != null)
            return true;

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        if (!_map.TryLoad(shuttleMap, ent.Comp.ShuttlePath, out _, options))
            return false;

        ent.Comp.ShuttleMap = _mapManager.GetMapEntityId(shuttleMap);
        return true;
    }

    private void SetupWizardEntity(
        EntityUid mob,
        string name,
        int points,
        StartingGearPrototype? gear,
        HumanoidCharacterProfile? profile)
    {
        _metaData.SetEntityName(mob, name);
        EnsureComp<WizardComponent>(mob);

        if (profile != null)
            _humanoid.LoadProfile(mob, profile);

        if (gear == null)
        {
            _prototypeManager.TryIndex("WizardStartingGear", out StartingGearPrototype? defaultGear); //TODO: actual gear proto id
            gear = defaultGear;
        }

        if (gear != null)
            _stationSpawning.EquipStartingGear(mob, gear, profile);

        _npcFaction.RemoveFaction(mob, "NanoTrasen", false);
        //_npcFaction.AddFaction(mob, "Syndicate"); //TODO: think about factions
    }

    private void SpawnWizard(ICommonSession? session, WizardRuleComponent component, bool spawnGhostRoles = true)
    {
        if (component.ShuttleMap is not { Valid: true } mapUid)
            return;

        var spawn = new EntityCoordinates();
        foreach (var (_, meta, xform) in EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != component.SpawnPointProto.Id)
                continue;

            if (xform.ParentUid != component.ShuttleMap)
                continue;

            spawn = xform.Coordinates;
            return;
        }

        //Fallback, spawn at the centre of the map
        if (spawn == new EntityCoordinates())
        {
            spawn = Transform(mapUid).Coordinates;
            _sawmill.Warning($"Fell back to default spawn for nukies!");
        }

        //Spawn the team

        var name = "wizard name"; //TODO: wizard name

        var wizardAntag = _prototypeManager.Index(component.WizardRoleProto);

        //If a session is available, spawn mob and transfer mind into it
        if (session != null)
        {
            var profile =
                _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;

            if (!_prototypeManager.TryIndex(profile?.Species ?? SharedHumanoidAppearanceSystem.DefaultSpecies,
                        out SpeciesPrototype? species))
            {
                species = _prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies);
            }

            var mob = Spawn(species.Prototype, spawn);
            _prototypeManager.TryIndex(component.StartingGear, out var gear);
            SetupWizardEntity(mob, name, component.Points, gear, profile);

            var newMind = _mind.CreateMind(session.UserId, name);
            _mind.SetUserId(newMind, session.UserId);
            _roles.MindAddRole(newMind,
                new NukeopsRoleComponent { PrototypeId = component.WizardRoleProto });

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
            wizardSpawner.Name = name;
            //TODO: maybe other params
        }
    }

    private void NotifyWizard(ICommonSession session, WizardComponent wizard, WizardRuleComponent wizardRule)
    {
        _antagSelection.SendBriefing(session,
            Loc.GetString("wizard-welcome"), Color.Red,
            wizardRule.GreetSoundNotification); //TODO: wizard briefing
    }

    /// <summary>
    /// Spawn wizard ghost role if this gamerule was started mid round
    /// </summary>
    private void SpawnWizardGhostRole(EntityUid uid, WizardRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!SpawnMap((uid, component)))
        {
            _sawmill.Info("Failed to load map for wizard");
            return;
        }

        ICommonSession? session = null;
        SpawnWizard(session,  component, true);
    }


}
