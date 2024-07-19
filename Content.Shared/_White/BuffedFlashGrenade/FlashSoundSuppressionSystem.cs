using Content.Shared.Inventory.Events;
using Content.Shared.Stunnable;

namespace Content.Shared._White.BuffedFlashGrenade;

public sealed class FlashSoundSuppressionSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<FlashSoundSuppressionComponent, GotEquippedEvent>(OnItemEquipped);
        SubscribeLocalEvent<FlashSoundSuppressionComponent, GotUnequippedEvent>(OnItemUnequipped);
    }

    private void OnItemEquipped(EntityUid uid, FlashSoundSuppressionComponent component, GotEquippedEvent args)
    {
        EnsureComp<FlashSoundSuppressionComponent>(args.Equipee);
    }

    private void OnItemUnequipped(EntityUid uid, FlashSoundSuppressionComponent component, GotUnequippedEvent args)
    {
        RemComp<FlashSoundSuppressionComponent>(args.Equipee);
    }

    public void Stun(EntityUid target, float duration)
    {
        if (HasComp<FlashSoundSuppressionComponent>(target))
            return;

        _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(duration / 1000f), true);
    }
}
