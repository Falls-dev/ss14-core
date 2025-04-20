using Content.Shared._White.Targeting;
using Content.Shared._White.Targeting.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server._White.Targeting.Systems;

public sealed class TargetingSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TargetingChangeBodyPartEvent>(OnTargetChange);
        SubscribeLocalEvent<TargetingComponent, MobStateChangedEvent>(OnMobStateChange);
    }

    private void OnTargetChange(TargetingChangeBodyPartEvent message, EntitySessionEventArgs args)
    {
        if (!TryComp<TargetingComponent>(GetEntity(message.Entity), out var target))
            return;

        target.TargetBodyPart = message.BodyPart;

        Dirty(GetEntity(message.Entity), target);
    }

    private void OnMobStateChange(EntityUid uid, TargetingComponent component, MobStateChangedEvent args)
    {
        var changed = false;

        if (args.NewMobState == MobState.Dead)
        {
            foreach (TargetingBodyParts part in Enum.GetValues(typeof(TargetingBodyParts)))
            {
                component.TargetIntegrities[part] = TargetIntegrity.Dead;
                changed = true;
            }
        }
        else if (args is { OldMobState: MobState.Dead, NewMobState: MobState.Alive or MobState.Critical })
        {
            component.TargetIntegrities = _bodySystem.GetBodyPartStatus(uid);
            changed = true;
        }

        if (changed)
        {
            Dirty(uid, component);
            RaiseNetworkEvent(new TargetingIntegrityChangeEvent(GetNetEntity(uid)), uid);
        }
    }
}
