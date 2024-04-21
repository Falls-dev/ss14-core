using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Standing.Systems;

// WD ADDED
public abstract partial class SharedStandingStateSystem
{
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private void InitializeColliding()
    {
        SubscribeLocalEvent<StandingStateComponent, ProjectileCollideAttemptEvent>(OnProjectileCollideAttempt);
        SubscribeLocalEvent<StandingStateComponent, HitscanHitAttemptEvent>(OnHitscanHitAttempt);
    }

    private void OnProjectileCollideAttempt(EntityUid uid, StandingStateComponent component,
        ref ProjectileCollideAttemptEvent args)
    {
        if (component.CurrentState is StandingState.Standing)
        {
            return;
        }

        if (!TryHit(uid, args.Component.Target))
        {
            args.Cancelled = true;
        }
    }

    private void OnHitscanHitAttempt(EntityUid uid, StandingStateComponent component, ref HitscanHitAttemptEvent args)
    {
        if (component.CurrentState is StandingState.Standing)
        {
            return;
        }

        if (!TryHit(uid, args.Target))
        {
            args.Cancelled = true;
        }
    }

    private bool TryHit(EntityUid uid, EntityUid? target)
    {
        if (_mobState.IsAlive(uid) && Random.NextFloat() < 0.3f)
        {
            // We should hit
            return true;
        }

        // Only hit if we're target
        return uid == target;
    }
}