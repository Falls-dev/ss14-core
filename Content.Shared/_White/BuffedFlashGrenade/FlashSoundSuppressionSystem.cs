using Content.Shared.Inventory;
using Content.Shared.Stunnable;

namespace Content.Shared._White.BuffedFlashGrenade;

public sealed class FlashSoundSuppressionSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashSoundSuppressionComponent, InventoryRelayedEvent<GetFlashbangedEvent>>(
            OnGetFlashbanged);
    }

    private void OnGetFlashbanged(Entity<FlashSoundSuppressionComponent> ent,
        ref InventoryRelayedEvent<GetFlashbangedEvent> args)
    {
        args.Args.Protected = true;
    }

    public void Stun(EntityUid target, float duration, float distance, float range)
    {
        if (range <= 0)
            return;
        if (distance < 0)
            distance = 0;
        if (distance > range)
            distance = range;

        var stunTime = float.Lerp(duration, 0f, distance / range);
        if (stunTime <= 0f)
            return;

        if (HasComp<FlashSoundSuppressionComponent>(target))
            return;

        var ev = new GetFlashbangedEvent();
        RaiseLocalEvent(target, ev);
        if (ev.Protected)
            return;

        _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(stunTime / 1000f), true);
    }
}

public sealed class GetFlashbangedEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool Protected;

    public SlotFlags TargetSlots => SlotFlags.EARS | SlotFlags.HEAD;
}
