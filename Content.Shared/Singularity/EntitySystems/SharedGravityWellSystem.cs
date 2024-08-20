using Content.Shared.Atmos.Components;
using Content.Shared.Ghost;
using Content.Shared.Singularity.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// The entity system primarily responsible for managing <see cref="SharedGravityWellComponent"/>s.
/// </summary>
public abstract class SharedGravityWellSystem : EntitySystem
{

    #region Dependencies
    [Dependency] public readonly IGameTiming _timing = default!;
    [Dependency] public readonly IViewVariablesManager _vvManager = default!;
    [Dependency] public readonly EntityLookupSystem _lookup = default!;
    [Dependency] public readonly SharedPhysicsSystem _physics = default!;
    [Dependency] public readonly SharedTransformSystem _transform = default!;
    [Dependency] public readonly SharedStunSystem _stun = default!; // WD EDIT
    #endregion Dependencies

    /// <summary>
    /// The minimum range at which gravpulses will act.
    /// Prevents division by zero problems.
    /// </summary>
    public const float MinGravPulseRange = 0.00001f;

    private const string BlockTagID = "GravityWellAllowStatic";

    /// <summary>
    /// Causes a gravitational pulse, shoving around all entities within some distance of an epicenter.
    /// </summary>
    /// <param name="mapPos">The epicenter of the gravity pulse.</param>
    /// <param name="maxRange">The maximum distance at which entities can be affected by the gravity pulse.</param>
    /// <param name="minRange">The minimum distance at which entities can be affected by the gravity pulse. Exists to prevent div/0 errors.</param>
    /// <param name="baseMatrixDeltaV">The base velocity added to any entities within affected by the gravity pulse scaled by the displacement of those entities from the epicenter.</param>
    public void GravPulse(MapCoordinates mapPos, float maxRange, float minRange, in Matrix3 baseMatrixDeltaV, float stunTime = 0f, List<EntityUid>? ignore = null)
    {
        if (mapPos == MapCoordinates.Nullspace)
            return; // No gravpulses in nullspace please.

        var epicenter = mapPos.Position;
        var minRange2 = MathF.Max(minRange * minRange, MinGravPulseRange); // Cache square value for speed. Also apply a sane minimum value to the minimum value so that div/0s don't happen.

        foreach(var entity in _lookup.GetEntitiesInRange(mapPos.MapId, epicenter, maxRange, flags: LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
        {
            if (!entity.Valid)
                continue;

            if (ignore?.Contains(entity) is true)
                continue;

            // WD added start
            if (!TryComp<PhysicsComponent>(entity, out var physics)) // WD edit
                continue;

            if (!TryComp(entity, out TransformComponent? xform))
                continue;

            if (xform.Anchored && TryComp<TagComponent>(entity, out var tag) && tag.Tags.Contains(BlockTagID))
                continue;
            // WD added end

            if (TryComp<MovedByPressureComponent>(entity, out var movedPressure) && !movedPressure.Enabled) // Ignore magboots users
                continue;

            if (!CanGravPulseAffect(entity))
                continue;

            if (xform.Anchored) // WD added
                _transform.Unanchor(entity, xform);

            var displacement = epicenter - _transform.GetWorldPosition(entity);
            var distance2 = displacement.LengthSquared();

            if (distance2 < minRange2)
                continue;

            var scaling = (1f / distance2) * physics.Mass; // TODO: Variable falloff gradiants.
            _physics.ApplyLinearImpulse(entity, (displacement * baseMatrixDeltaV) * scaling, body: physics);

            if (stunTime > 0f)
                _stun.TryParalyze(entity, TimeSpan.FromSeconds(stunTime), true);
        }
    }

    /// <summary>
    /// Checks whether an entity can be affected by gravity pulses.
    /// TODO: Make this an event or such.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    private bool CanGravPulseAffect(EntityUid entity)
    {
        return !(
            HasComp<GhostComponent>(entity) ||
            HasComp<MapGridComponent>(entity) ||
            HasComp<MapComponent>(entity) ||
            HasComp<GravityWellComponent>(entity)
        );
    }
}
