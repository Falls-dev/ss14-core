using Content.Shared.Borer;
using Content.Shared.Mobs;

namespace Content.Server.Borer;


public sealed class ServerBorerHostSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BorerHostComponent, MobStateChangedEvent>(OnDamageChanged);
    }

    [Obsolete("Obsolete")]
    private void OnDamageChanged(EntityUid uid, BorerHostComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            var ev = new BorerOutActionEvent();
            RaiseLocalEvent(component.Borer, ev, true);

        } else if (args.NewMobState == MobState.Critical)
        {
            RaiseLocalEvent(uid, new BorerBrainReleaseEvent(), true);

        }
    }
}
