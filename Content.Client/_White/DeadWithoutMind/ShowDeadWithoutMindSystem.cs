using Content.Client.Mind;
using Content.Client.Overlays;
using Content.Shared._White.DeadWithoutMind;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._White.DeadWithoutMind;

public sealed class ShowDeadWithoutMindSystem : EquipmentHudSystem<ShowDeadWithoutMindComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(Entity<MobStateComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive || args.InContainer)
            return;

        if(_mindSystem.TryGetMind(entity.Owner, out _, out _))
            return;

        if(entity.Comp.CurrentState is MobState.Alive or MobState.Critical)
            return;

        if (_prototype.TryIndex<StatusIconPrototype>(entity.Comp.DeadWithoutMindIcon.Id, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
