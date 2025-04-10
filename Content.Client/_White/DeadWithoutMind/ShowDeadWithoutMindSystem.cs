using Content.Client.Overlays;
using Content.Shared._White.DeadWithoutMind;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._White.DeadWithoutMind;

public sealed class ShowDeadWithoutMindSystem : EquipmentHudSystem<ShowDeadWithoutMindComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(Entity<MobStateComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive || args.InContainer)
            return;

        if(!TryComp<MindContainerComponent>(entity.Owner, out var mindContainer))
            return;

        var dead = _mobStateSystem.IsDead(entity.Owner);
        var hasUserId = CompOrNull<MindComponent>(mindContainer.Mind)?.UserId;

        if (!dead || hasUserId != null)
            return;

        if (_prototype.TryIndex<StatusIconPrototype>(entity.Comp.DeadWithoutMindIcon.Id, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
