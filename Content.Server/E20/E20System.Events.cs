using System.Numerics;
using Content.Server.Administration.Systems;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Revenant.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.E20;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    //private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<E20Component, ExaminedEvent>(DieEvent);
        //IoCManager.Resolve<BodySystem>();

    }

    public void ExplosionEvent(EntityUid uid, E20Component comp)
    {
        float intensity = comp.CurrentValue * 280; // Calculating power of explosion


        if (comp.CurrentValue == 20) // Critmass-like explosion
        {
            _explosion.TriggerExplosive(uid, totalIntensity:intensity*15, radius:_cfgManager.GetCVar(CCVars.ExplosionMaxArea));
            return;
        }

        if (comp.CurrentValue == 1)
        {
            //TransformSystem ts = new TransformSystem();
            MapCoordinates coords = Transform(comp.LastUser).MapPosition;
            //MapCoordinates coords = Transform(comp.LastUser).MapPosition;

            _explosion.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                4,1,2,0); // Small explosion for the sake of appearance
            _bodySystem.GibBody(comp.LastUser, true); // gibOrgans=true dont gibs the organs
            return;
        }

        _explosion.TriggerExplosive(uid, totalIntensity:intensity);
    }

    public void FullDestruction(EntityUid uid, E20Component comp)
    {
        _bodySystem.GibBody(comp.LastUser);
    }

    public void Die(EntityUid uid, E20Component comp)
    {
        DamageSpecifier damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 200);
        _damageableSystem.TryChangeDamage(comp.LastUser, damage, true);
    }

    public void AngryMobsSpawn(EntityUid uid, E20Component comp)
    {
        //TransformSystem ts = new TransformSystem();
        MapCoordinates coords = Transform(uid).MapPosition;
        EntityManager.SpawnEntities(coords, "MobCarpDungeon", 5);
    }

    public void ItemsDestruction(EntityUid uid, E20Component comp)
    {
        EntityUid bodyId = comp.LastUser;
        if (!TryComp<InventoryComponent>(comp.LastUser, out var inventory))
            return;
        foreach (var item in _inventory.GetHandOrInventoryEntities(bodyId))
        {
            QueueDel(item);
        }
    }

    public void MonkeyPolymorph(EntityUid uid, E20Component comp)
    {
        IoCManager.Register<PolymorphSystem>();
        _polymorphSystem.PolymorphEntity(comp.LastUser, "AdminMonkeySmite");
    }

    public void SpeedReduce(EntityUid uid, E20Component comp)
    {
        if (!TryComp<MovementSpeedModifierComponent>(comp.LastUser, out var movementSpeed))
            return;
        float newSprint = movementSpeed.BaseSprintSpeed / 2;
        float newWalk = movementSpeed.BaseWalkSpeed / 2;

        _movementSpeedModifier.ChangeBaseSpeed(
            comp.LastUser,
            baseWalkSpeed:newWalk,
            baseSprintSpeed:newSprint,
            acceleration:movementSpeed.Acceleration);
    }

    public void Throwing(EntityUid uid, E20Component comp)
    {

        MapCoordinates diceCoords = Transform(uid).MapPosition;
        MapCoordinates playerCoords = Transform(comp.LastUser).MapPosition;

        var direction = diceCoords.Position - playerCoords.Position;
        var randomizedDirection = direction + new Vector2(_random.Next(-10, 10), _random.Next(-10, 10));
        _throwingSystem.TryThrow(comp.LastUser, randomizedDirection, 60, uid);

        DamageSpecifier damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 50);
        _damageableSystem.TryChangeDamage(comp.LastUser, damage, true);

        if (!TryComp<StaminaComponent>(comp.LastUser, out var staminaComponent))
            return;
        _stamina.TakeStaminaDamage(comp.LastUser, staminaComponent.CritThreshold);
    }

    public void Disease(EntityUid uid, E20Component comp)
    {
        var blight = EnsureComp<BlightComponent>(comp.LastUser);
        blight.Duration = 0f;
    }

    public void Nothing(EntityUid uid, E20Component comp)
    {

    }

    public void Cookie(EntityUid uid, E20Component comp)
    {
        MapCoordinates coords = Transform(uid).MapPosition;
        EntityManager.SpawnEntities(coords, "FoodBakedCookie", 2);
    }

    public void RejuvenateEvent(EntityUid uid, E20Component comp)
    {
        _rejuvenate.PerformRejuvenate(comp.LastUser);
    }

    public void Money(EntityUid uid, E20Component comp)
    {
        MapCoordinates coords = Transform(uid).MapPosition;
        EntityManager.SpawnEntities(coords, "SpaceCash1000", 1);
    }

    public void Revolver(EntityUid uid, E20Component comp)
    {
        MapCoordinates coords = Transform(uid).MapPosition;
        EntityManager.SpawnEntities(coords, "WeaponRevolverInspector", 1);
    }

    public void MagicWand(EntityUid uid, E20Component comp)
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

        var roll = _random.Next(0, wands.Count);
        MapCoordinates coords = Transform(uid).MapPosition;
        EntityManager.SpawnEntities(coords, wands[roll], 1);
    }
}
