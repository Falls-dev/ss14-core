using System;
using Content.Shared.Borer;
using Content.Shared.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.Borer;

/// <summary>
/// This handles...
/// </summary>
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
            var ev = new BorerOutActionEvent();
            RaiseLocalEvent(uid, new BorerBrainReleaseEvent(), true);

        }
    }
}
