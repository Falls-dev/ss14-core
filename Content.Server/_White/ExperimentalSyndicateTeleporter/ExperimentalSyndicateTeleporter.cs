using System.Linq;
using System.Numerics;
using Content.Server._White.Other;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._White.ExperimentalSyndicateTeleporter;
public sealed class ExperimentalSyndicateTeleporter : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExperimentalSyndicateTeleporterComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<ExperimentalSyndicateTeleporterComponent, ExaminedEvent>(OnExamine);
    }

    private void OnUse(EntityUid uid, ExperimentalSyndicateTeleporterComponent component, UseInHandEvent args)
    {
        if (component.Uses <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("experimental-syndicate-teleporter-end-uses"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (component.NextUse > _timing.CurTime)
        {
            _popupSystem.PopupEntity(Loc.GetString("experimental-syndicate-teleporter-cooldown"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!TryComp<TransformComponent>(args.User, out var xform))
            return;

        var oldCoords = xform.Coordinates;

        int random = new Random().Next(component.MinTeleportRange, component.MaxTeleportRange);
        Vector2 offset = xform.LocalRotation.ToWorldVec().Normalized();
        Vector2 direction = xform.LocalRotation.GetDir().ToVec();
        Vector2 newOffset = offset + direction * random;

        EntityCoordinates coords = xform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

        if (TryCheckWall(coords))
        {
            EmergencyTeleportation(args.User, xform, component, oldCoords);
            return;
        }

        if (TryCheckForMob(coords) != default)
        {
            EntityUid entity = TryCheckForMob(coords);
            _stunSystem.TryStun(entity, TimeSpan.FromSeconds(3), true);
            _damageableSystem.TryChangeDamage(entity,
                new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Brute"), -20), true);
        }

        SoundAndEffects(component, coords, oldCoords);

        _transform.SetCoordinates(args.User, coords);

        component.Uses--;
        component.NextUse = _timing.CurTime + component.Cooldown;
    }

    private void OnExamine(EntityUid uid, ExperimentalSyndicateTeleporterComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("experimental-syndicate-teleporter-examine", ("uses", component.Uses)));
    }

    private void EmergencyTeleportation(EntityUid uid, TransformComponent xform, ExperimentalSyndicateTeleporterComponent component, EntityCoordinates oldCoords)
    {
        Vector2 offset = xform.LocalRotation.ToWorldVec().Normalized();
        Vector2 newOffset = offset + VectorRandomDirection(component, offset, component.EmergencyLength);

        EntityCoordinates coords = xform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

        SoundAndEffects(component, coords, oldCoords);

        _transform.SetCoordinates(uid, coords);

        component.Uses--;
        component.NextUse = _timing.CurTime + component.Cooldown;

        if (TryCheckForMob(coords) != default)
        {
            EntityUid entity = TryCheckForMob(coords);
            _stunSystem.TryStun(entity, TimeSpan.FromSeconds(3), true);
            _damageableSystem.TryChangeDamage(entity,
                new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Brute"), -20), true);
        }

        if (TryCheckWall(coords))
        {
            _bodySystem.GibBody(uid, true, splatModifier: 3F);
        }
    }

    private void SoundAndEffects(ExperimentalSyndicateTeleporterComponent component, EntityCoordinates coords, EntityCoordinates oldCoords)
    {
        _audio.PlayPvs(component.TeleportSound, coords);
        _audio.PlayPvs(component.TeleportSound, oldCoords);

        _entManager.SpawnEntity(component.ExpSyndicateTeleportInEffect, coords);
        _entManager.SpawnEntity(component.ExpSyndicateTeleportOutEffect, oldCoords);
    }

    private bool TryCheckWall(EntityCoordinates coords)
    {
        if (!coords.TryGetTileRef(out var tile))
            return false;

        if (!TryComp<MapGridComponent>(tile.Value.GridUid, out var mapGridComponent))
            return false;

        var anchoredEntities = _mapSystem.GetAnchoredEntities(tile.Value.GridUid, mapGridComponent, coords);

        return anchoredEntities.Any(HasComp<WallMarkComponent>);
    }

    private EntityUid TryCheckForMob(EntityCoordinates coords)
    {
        var entities = _entityLookupSystem.GetEntitiesIntersecting(coords);

        foreach (var entity in entities.Where(HasComp<MobStateComponent>))
        {
            return entity;
        }

        return default;
    }

    private Vector2 VectorRandomDirection(ExperimentalSyndicateTeleporterComponent component, Vector2 offset, int length)
    {
        int randomRotation = new Random().Next(0, component.RandomRotations.Count);
        return Angle.FromDegrees(component.RandomRotations[randomRotation]).RotateVec(offset.Normalized() * length);
    }
}
