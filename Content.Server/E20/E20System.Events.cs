using System.Numerics;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Systems;
using Content.Server.Body.Systems;
using Content.Server.Changeling;
using Content.Server.Chat.Managers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Revenant.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.E20;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.E20;

public sealed class E20SystemEvents : EntitySystem
{

    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly GhostRoleSystem _ghost = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ChangelingRuleSystem _changelingRule = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    //[Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        IoCManager.Register<PolymorphSystem>();
        base.Initialize();
        SubscribeLocalEvent<GhostRoleComponent, TakeGhostRoleEvent>(OnTake);
    }

    public void ExplosionEvent(EntityUid uid, E20Component comp)
    {
        float intensity = comp.CurrentValue * 280; // Calculating power of explosion

        switch (comp.CurrentValue)
        {
            // Critmass-like explosion
            case 20:
                _explosion.TriggerExplosive(uid, totalIntensity:intensity*15, radius:_cfgManager.GetCVar(CCVars.ExplosionMaxArea));
                return;
            case 1:
            {
                var coords = _transform.GetMapCoordinates(comp.LastUser);

                _explosion.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                    4,1,2,0); // Small explosion for the sake of appearance
                _bodySystem.GibBody(comp.LastUser, true); // gibOrgans=true dont gibs the organs
                return;
            }
            default:
                _explosion.TriggerExplosive(uid, totalIntensity:intensity);
                break;
        }
    }

    public void FullDestructionEvent(EntityUid uid, E20Component comp)
    {
        _bodySystem.GibBody(comp.LastUser);

        if (!TryComp<MindContainerComponent>(comp.LastUser, out var targetMindComp))
            return;

        if (!_minds.TryGetSession(targetMindComp.Mind, out var session))
            return;

        var minds = _entities.System<SharedMindSystem>();
        if (!minds.TryGetMind(session, out var mindId, out var mind))
        {
            mindId = minds.CreateMind(session.UserId);
        }
    }

    public void DieEvent(EntityUid uid, E20Component comp)
    {
        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 200);
        _damageableSystem.TryChangeDamage(comp.LastUser, damage, true);
    }

    public void AngryMobsSpawnEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);
        EntityManager.SpawnEntities(coords, "MobCarpDungeon", 5);
    }

    public void ItemsDestructionEvent(EntityUid uid, E20Component comp)
    {
        var bodyId = comp.LastUser;
        if (!TryComp<InventoryComponent>(comp.LastUser, out var inventory))
            return;
        foreach (var item in _inventory.GetHandOrInventoryEntities(bodyId))
        {
            QueueDel(item);
        }
    }

    public void MonkeyPolymorphEvent(EntityUid uid, E20Component comp)
    {
        _polymorphSystem.PolymorphEntity(comp.LastUser, "AdminMonkeySmite");
    }

    public void SpeedReduceEvent(EntityUid uid, E20Component comp)
    {
        if (!TryComp<MovementSpeedModifierComponent>(comp.LastUser, out var movementSpeed))
            return;

        var newSprint = movementSpeed.BaseSprintSpeed / 2;
        var newWalk = movementSpeed.BaseWalkSpeed / 2;

        _movementSpeedModifier.ChangeBaseSpeed(
            comp.LastUser,
            baseWalkSpeed:newWalk,
            baseSprintSpeed:newSprint,
            acceleration:movementSpeed.Acceleration);
    }

    public void ThrowingEvent(EntityUid uid, E20Component comp)
    {
        var diceCoords = _transform.GetMapCoordinates(uid);
        var playerCoords = _transform.GetMapCoordinates(comp.LastUser);

        var direction = diceCoords.Position - playerCoords.Position;
        var randomizedDirection = direction + new Vector2(_random.Next(-10, 10), _random.Next(-10, 10));

        _throwingSystem.TryThrow(comp.LastUser, randomizedDirection, 60, uid);

        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 50);

        _damageableSystem.TryChangeDamage(comp.LastUser, damage, true);

        if (!TryComp<StaminaComponent>(comp.LastUser, out var staminaComponent))
            return;

        _stamina.TakeStaminaDamage(comp.LastUser, staminaComponent.CritThreshold);
    }

    public void DiseaseEvent(EntityUid uid, E20Component comp)
    {
        var blight = EnsureComp<BlightComponent>(comp.LastUser);
        blight.Duration = 0f;
    }

    public void NothingEvent(EntityUid uid, E20Component comp)
    {

    }

    public void CookieEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);
        EntityManager.SpawnEntities(coords, "FoodBakedCookie", 2);
    }

    public void RejuvenateEvent(EntityUid uid, E20Component comp)
    {
        _rejuvenate.PerformRejuvenate(comp.LastUser);
    }

    public void MoneyEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);
        EntityManager.SpawnEntities(coords, "SpaceCash1000", 5);
    }

    public void RevolverEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);
        EntityManager.SpawnEntities(coords, "WeaponRevolverInspector", 1);
    }

    public void MagicWandEvent(EntityUid uid, E20Component comp)
    {
        List<string> wands = new List<string>()
        {
            "WeaponWandPolymorphDoor",
            "WeaponWandDeath",
            "WeaponWandFireball",
            "WeaponWandPolymorphCarp",
            "WeaponWandPolymorphMonkey",
            "WeaponWandPolymorphBread",
            "WeaponWandCluwne",
            "FoodFrozenPopsicleTrash" // lol
        };

        var roll = _random.Pick(wands);
        var coords = _transform.GetMapCoordinates(uid);
        EntityManager.SpawnEntities(coords, roll, 1);
    }


    public void SlaveEvent(EntityUid uid, E20Component comp)
    {
        var ghost = EnsureComp<GhostRoleComponent>(uid);

        ghost.RoleName = Loc.GetString("osel");
        ghost.RoleDescription = Loc.GetString("eat grass");
    }

    private void OnTake(EntityUid uid, GhostRoleComponent comp, TakeGhostRoleEvent args)
    {
        //TODO: нельзя чтобы через кнопку сделать рользью призрака он тригерился об этот ивент
        var coords = _transform.GetMapCoordinates(uid);
        var ghost = EnsureComp<GhostRoleComponent>(uid);
        var spawnPoint = Spawn("MobHuman", coords);

        _ghost.GhostRoleInternalCreateMindAndTransfer(args.Player, uid, spawnPoint, ghost);

        var mind = EnsureComp<MindComponent>(spawnPoint);
        var meta = MetaData(spawnPoint);

        _metaData.SetEntityName(spawnPoint, "твоя мама", meta);
        mind.CharacterName = "Артур пирожков нахуй";
        SetOutfitCommand.SetOutfit(spawnPoint, "LibrarianGear", EntityManager);

        _chat.DispatchServerMessage(args.Player,"Служите вашей маме");
        _ghost.UnregisterGhostRole((uid, ghost));
    }

    public void RandomSyndieBundleEvent(EntityUid uid, E20Component comp)
    {
        List<string> bundles = new List<string>()
        {
            "ClothingBackpackDuffelSyndicateFilledSMG",
            "ClothingBackpackDuffelSyndicateFilledShotgun",
            "CrateSyndicateSurplusBundle",
            "BriefcaseSyndieSniperBundleFilled",
            "ClothingBackpackDuffelSyndicateFilledGrenadeLauncher",
            "ClothingBackpackDuffelSyndicateFilledLMG",
            "CrateSyndicateSuperSurplusBundle"
        };

        Solution sol = new();
        var smoke = EnsureComp<SmokeComponent>(uid);
        var roll = _random.Pick(bundles);
        var coords = _transform.GetMapCoordinates(uid);

        Spawn("Smoke", coords);
        _smoke.StartSmoke(uid, sol, 5f, 5, smoke);

        EntityManager.SpawnEntities(coords, roll, 1);
    }

    public void FullAccessEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);
        EntityManager.SpawnEntities(coords, "CaptainIDCard", 1);
    }

    public void DamageResistEvent(EntityUid uid, E20Component comp)
    {
        var damageSet = "DiceOfFate";
        _damageable.SetDamageModifierSetId(comp.LastUser, damageSet);
    }

    public void ChangelingTransformationEvent(EntityUid uid, E20Component comp)
    {
        if (!TryComp<MindContainerComponent>(comp.LastUser, out var targetMindComp))
            return;

        if (!_minds.TryGetSession(targetMindComp.Mind, out var session))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(comp.LastUser, out var isHuman))
        {
            return;
        }

        _changelingRule.MakeChangeling(session);
    }
}
