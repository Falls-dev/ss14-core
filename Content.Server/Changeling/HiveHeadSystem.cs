using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Inventory;

namespace Content.Server.Changeling;

public sealed class HiveHeadSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiveHeadComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HiveHeadComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HiveHeadComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HiveHeadComponent, ReleaseBeesEvent>(OnReleaseBees);
    }

    private void OnReleaseBees(Entity<HiveHeadComponent> ent, ref ReleaseBeesEvent args)
    {
        args.Handled = true;

        var coords = Transform(args.Performer).Coordinates;

        for (var i = 0; i < ent.Comp.BeesAmount; i++)
        {
            Spawn(ent.Comp.BeeProto, coords);
        }

        _popup.PopupEntity(Loc.GetString("changeling-ability-bees-popup"), args.Performer, args.Performer);
    }

    private void OnGetActions(Entity<HiveHeadComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags == SlotFlags.HEAD)
            args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<HiveHeadComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent, ent.Comp.ActionEntity);
    }

    private void OnMapInit(Entity<HiveHeadComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }
}
