using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;

namespace Content.Shared._White.Item.PseudoItem;

public abstract class SharedPseudoItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PseudoItemComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
    }

    private void OnGettingPickedUpAttempt(Entity<PseudoItemComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        args.Cancel();
        OnGettingPickedUp(ent, args);
    }

    protected virtual void OnGettingPickedUp(Entity<PseudoItemComponent> ent, GettingPickedUpAttemptEvent args) {}
}

public sealed class PseudoItemInteractEvent(EntityUid used, EntityUid user)
    : EntityEventArgs
{
    public EntityUid Used { get; } = used;
    public EntityUid User { get; } = user;
}
