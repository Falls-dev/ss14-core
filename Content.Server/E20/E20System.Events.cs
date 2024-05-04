using System.Linq;
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
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Dataset;
using Content.Shared.E20;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
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
    [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ChangelingRuleSystem _changelingRule = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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
        float intensity = comp.CurrentValue * 480; // Calculating power of explosion
        var coords = _transform.GetMapCoordinates(uid);

        switch (comp.CurrentValue)
        {
            case 1:
                coords = _transform.GetMapCoordinates(comp.LastUser); // Explode user instead of dice
                intensity = 100;
                _bodySystem.GibBody(comp.LastUser, true); // gibOrgans=true dont gibs the organs
                break;
            case 20:
                intensity *= 10;
                break;
        }

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-explosion-event"),
            Transform(uid).Coordinates, PopupType.Medium);
        _explosion.QueueExplosion(coords, "DemolitionCharge",
            intensity, 5, 240);
    }

    public void FullDestructionEvent(EntityUid uid, E20Component comp)
    {
        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-full-destruction-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        _bodySystem.GibBody(comp.LastUser);
    }

    public void DieEvent(EntityUid uid, E20Component comp)
    {
        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 200);

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-die-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        _damageableSystem.TryChangeDamage(comp.LastUser, damage, true);
    }

    public void AngryMobsSpawnEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-angry-mobs-spawn-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
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

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-items-destruction-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
    }

    public void MonkeyPolymorphEvent(EntityUid uid, E20Component comp)
    {
        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-monkey-polymorph-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
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

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-speed-reduce-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
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

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-throwing-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        _stamina.TakeStaminaDamage(comp.LastUser, staminaComponent.CritThreshold);
    }

    public void DiseaseEvent(EntityUid uid, E20Component comp)
    {
        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-disease-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        var blight = EnsureComp<BlightComponent>(comp.LastUser);
        blight.Duration = 0f;
    }

    public void NothingEvent(EntityUid uid, E20Component comp)
    {
        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-nothing-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
    }

    public void CookieEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-cookie-event"),
            Transform(uid).Coordinates, PopupType.Medium);
        EntityManager.SpawnEntities(coords, "FoodBakedCookie", 2);
    }

    public void RejuvenateEvent(EntityUid uid, E20Component comp)
    {
        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-rejuvenate-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        _rejuvenate.PerformRejuvenate(comp.LastUser);
    }

    public void MoneyEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-money-event"),
            Transform(uid).Coordinates, PopupType.Medium);
        EntityManager.SpawnEntities(coords, "SpaceCash1000", 5);
    }

    public void RevolverEvent(EntityUid uid, E20Component comp)
    {
        var coords = _transform.GetMapCoordinates(uid);

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-revolver-event"),
            Transform(uid).Coordinates, PopupType.Medium);
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

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-magic-wand-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        EntityManager.SpawnEntities(coords, roll, 1);
    }


    public void SlaveEvent(EntityUid uid, E20Component comp)
    {
        var ghost = EnsureComp<GhostRoleComponent>(uid);

        ghost.RoleName = Loc.GetString("dice-of-fate-slave-role-name");
        ghost.RoleDescription = Loc.GetString("dice-of-fate-slave-role-description");

        comp.IsUsed = false; // We dont want to polymorph Dice before the player takes the role

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-slave-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
    }

    private void OnTake(EntityUid uid, GhostRoleComponent comp, TakeGhostRoleEvent args)
    {
        if (!HasComp<E20Component>(uid))
        {
            return;
        }

        var e20 = EnsureComp<E20Component>(uid);
        var name = _random.Pick(_prototypeManager.Index<DatasetPrototype>("names_death_commando").Values);
        var ghost = EnsureComp<GhostRoleComponent>(uid);
        var coords = _transform.GetMapCoordinates(uid);
        var mob = Spawn("MobHuman", coords);
        var meta = MetaData(mob);

        _metaData.SetEntityName(mob, name, meta);
        SetOutfitCommand.SetOutfit(mob, "LibrarianGear", EntityManager);
        var newMind = _minds.CreateMind(args.Player.UserId, name);
        _minds.SetUserId(newMind, args.Player.UserId);
        _minds.TransferTo(newMind, mob);
        _chat.DispatchServerMessage(args.Player,Loc.GetString("dice-of-fate-slave-server-message",
            ("user", Identity.Entity(e20.LastUser, _entManager))));

        _ghost.UnregisterGhostRole((uid, ghost));
        _polymorphSystem.PolymorphEntity(uid, "DiceShard");
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
        _smoke.StartSmoke(uid, sol, 2f, 5, smoke);

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-random-syndie-bundle-event"),
            Transform(uid).Coordinates, PopupType.Medium);
        EntityManager.SpawnEntities(coords, roll, 1);
    }

    public void FullAccessEvent(EntityUid uid, E20Component comp)
    {
        EnsureComp<AccessComponent>(comp.LastUser);
        var allAccess = _prototypeManager
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Select(p => new ProtoId<AccessLevelPrototype>(p.ID)).ToArray();


        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-full-access-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        _accessSystem.TrySetTags(comp.LastUser, allAccess);
    }

    public void DamageResistEvent(EntityUid uid, E20Component comp)
    {
        var damageSet = "DiceOfFate";

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-damage-resist-event",
            ("user", Identity.Entity(comp.LastUser, _entManager))) , Transform(uid).Coordinates, PopupType.Medium);
        _damageable.SetDamageModifierSetId(comp.LastUser, damageSet);
    }

    public void ChangelingTransformationEvent(EntityUid uid, E20Component comp)
    {
        if (!TryComp<MindContainerComponent>(comp.LastUser, out var targetMindComp))
            return;

        if (!_minds.TryGetSession(targetMindComp.Mind, out var session))
            return;

        _popup.PopupCoordinates(Loc.GetString("dice-of-fate-changeling-transformation-event"),
            Transform(uid).Coordinates, PopupType.Medium);
        _changelingRule.MakeChangeling(comp.LastUser, new ChangelingRuleComponent(), false);
    }
}
