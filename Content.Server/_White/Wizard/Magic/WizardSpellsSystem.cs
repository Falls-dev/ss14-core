using System.Linq;
using System.Numerics;
using Content.Server._White.Wizard.Magic.Amaterasu;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Lightning;
using Content.Server.Magic;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._White.Wizard;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Magic;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server._White.Wizard.Magic;

public sealed class WizardSpellsSystem : EntitySystem
{
    #region Dependencies

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly MagicSystem _magicSystem = default!;
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardsSpellEvent>(OnCardsSpell);
        SubscribeLocalEvent<FireballSpellEvent>(OnFireballSpell);
        SubscribeLocalEvent<ForceSpellEvent>(OnForceSpell);
        SubscribeLocalEvent<ArcSpellEvent>(OnArcSpell);
    }

    #region Cards

    private void OnCardsSpell(CardsSpellEvent msg)
    {
        if (msg.Handled)
            return;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                CardsSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                CardsSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                CardsSpellAlt(msg);
                break;
        }

        msg.Handled = true;
        Speak(msg);
    }

    private void CardsSpellDefault(CardsSpellEvent msg)
    {
        var xform = Transform(msg.Performer);

        for (var i = 0; i < 10; i++)
        {
            foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
            {
                var mapPos = _transformSystem.ToMapCoordinates(pos);
                var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                    ? pos.WithEntityId(gridUid, EntityManager)
                    : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

                var ent = Spawn(msg.Prototype, spawnCoords);

                var direction = msg.Target.ToMapPos(EntityManager, _transformSystem) - spawnCoords.ToMapPos(EntityManager, _transformSystem);
                var randomizedDirection = direction + new Vector2(_random.Next(-2, 2), _random.Next(-2, 2));

                _throwingSystem.TryThrow(ent, randomizedDirection, 60, msg.Performer);
            }
        }
    }

    private void CardsSpellCharge(CardsSpellEvent msg)
    {
        var xform = Transform(msg.Performer);

        var count = 5 * msg.ChargeLevel;
        var angleStep = 360f / count;

        for (var i = 0; i < count; i++)
        {
            var angle = i * angleStep;

            var direction = new Vector2(MathF.Cos(MathHelper.DegreesToRadians(angle)), MathF.Sin(MathHelper.DegreesToRadians(angle)));

            foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
            {
                var mapPos = _transformSystem.ToMapCoordinates(pos);

                var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                    ? pos.WithEntityId(gridUid, EntityManager)
                    : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

                var ent = Spawn(msg.Prototype, spawnCoords);

                _throwingSystem.TryThrow(ent, direction, 60, msg.Performer);
            }
        }
    }

    private void CardsSpellAlt(CardsSpellEvent msg)
    {
        if (!HasComp<ItemComponent>(msg.TargetUid))
            return;

        Del(msg.TargetUid);
        var item = Spawn(msg.Prototype);
        _handsSystem.TryPickupAnyHand(msg.Performer, item);
    }

    #endregion

    #region Fireball

    private void OnFireballSpell(FireballSpellEvent msg)
    {
        if (msg.Handled)
            return;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                FireballSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                FireballSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                FireballSpellAlt(msg);
                break;
        }

        msg.Handled = true;
        Speak(msg);
    }

    private void FireballSpellDefault(FireballSpellEvent msg)
    {
        var xform = Transform(msg.Performer);

        foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
        {
            var mapPos = _transformSystem.ToMapCoordinates(pos);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out var grid)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var userVelocity = Vector2.Zero;

            if (grid != null && TryComp(gridUid, out PhysicsComponent? physics))
                userVelocity = physics.LinearVelocity;

            var ent = Spawn(msg.Prototype, spawnCoords);
            var direction = msg.Target.ToMapPos(EntityManager, _transformSystem) - spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, msg.Performer, msg.Performer);
        }
    }

    private void FireballSpellCharge(FireballSpellEvent msg)
    {
        var coords = Transform(msg.Performer).Coordinates;

        var targets = _lookup.GetEntitiesInRange<FlammableComponent>(coords, 2 * msg.ChargeLevel);

        foreach (var target in targets.Where(target => target.Owner != msg.Performer))
        {
            target.Comp.FireStacks += 3;
            _flammableSystem.Ignite(target, msg.Performer);
        }
    }

    private void FireballSpellAlt(FireballSpellEvent msg)
    {
        if (!TryComp<FlammableComponent>(msg.TargetUid, out var flammableComponent))
            return;

        flammableComponent.FireStacks += 3;

        _flammableSystem.Ignite(msg.TargetUid, msg.Performer);

        EnsureComp<AmaterasuComponent>(msg.TargetUid);
    }

    #endregion

    #region Force

    private void OnForceSpell(ForceSpellEvent msg)
    {
        if (msg.Handled)
            return;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                ForceSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                ForceSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                ForceSpellAlt(msg);
                break;
        }

        msg.Handled = true;
        Speak(msg);
    }

    private void ForceSpellDefault(ForceSpellEvent msg)
    {
        Spawn("AdminInstantEffectMinusGravityWell", msg.Target);
    }

    private void ForceSpellCharge(ForceSpellEvent msg)
    {
        _gravityWell.GravPulse(msg.Performer, 15, 0, -80 * msg.ChargeLevel, -2 * msg.ChargeLevel);
    }

    private void ForceSpellAlt(ForceSpellEvent msg)
    {
        _gravityWell.GravPulse(msg.Target, 10, 0, 200, 10);
    }

    #endregion

    #region Arc

    private void OnArcSpell(ArcSpellEvent msg)
    {
        if (msg.Handled)
            return;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                ArcSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                ArcSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                ArcSpellAlt(msg);
                break;
        }

        msg.Handled = true;
        Speak(msg);
    }

    private void ArcSpellDefault(ArcSpellEvent msg)
    {
        const int possibleEntitiesCount = 2;

        var entitiesInRange = _lookup.GetEntitiesInRange(msg.Target, 1);
        var entitiesToHit = entitiesInRange.Where(HasComp<MobStateComponent>).Take(possibleEntitiesCount);

        foreach (var entity in entitiesToHit)
        {
            _lightning.ShootLightning(msg.Performer, entity);
        }
    }

    private void ArcSpellCharge(ArcSpellEvent msg)
    {
        _lightning.ShootRandomLightnings(msg.Performer, 2 * msg.ChargeLevel, msg.ChargeLevel * 2, arcDepth: 2);
    }

    private void ArcSpellAlt(ArcSpellEvent msg)
    {
        var xform = Transform(msg.Performer);

        foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
        {
            var mapPos = _transformSystem.ToMapCoordinates(pos);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out var grid)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var userVelocity = Vector2.Zero;

            if (grid != null && TryComp(gridUid, out PhysicsComponent? physics))
                userVelocity = physics.LinearVelocity;

            var ent = Spawn(msg.Prototype, spawnCoords);
            var direction = msg.Target.ToMapPos(EntityManager, _transformSystem) - spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, msg.Performer, msg.Performer);
        }
    }

    #endregion

    #region Helpers

    private void Speak(BaseActionEvent args)
    {
        if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);
    }

    #endregion
}
