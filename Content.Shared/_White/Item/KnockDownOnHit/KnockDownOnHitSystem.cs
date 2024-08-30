using Content.Shared.Damage.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Stunnable;

namespace Content.Shared._White.Item.KnockDownOnHit;

public sealed class KnockDownOnHitSystem : EntitySystem
{

    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockDownOnHitComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        SubscribeLocalEvent<KnockDownOnHitComponent, StaminaMeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<KnockDownOnHitComponent> ent, ref StaminaMeleeHitEvent args)
    {
        var time = ent.Comp.KnockdownTime;
        if (time <= TimeSpan.Zero)
            return;

        foreach (var (uid, _) in args.HitList)
        {
            _stun.TryKnockdown(uid, time, true, behavior: ent.Comp.KnockDownBehavior);
        }
    }

    private void OnStaminaHitAttempt(Entity<KnockDownOnHitComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
    {
        if (!_itemToggle.IsActivated(entity.Owner))
            args.Cancelled = true;
    }
}
