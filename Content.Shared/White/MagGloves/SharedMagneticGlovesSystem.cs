using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Shared.White.MagGloves;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedMagneticGlovesSystem : EntitySystem

{
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MagneticGlovesComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MagneticGlovesComponent, ToggleMagneticGlovesEvent>(OnToggleGloves);
    }

    public void OnGetActions(EntityUid uid, MagneticGlovesComponent component, GetItemActionsEvent args)
    {
        if (!args.InHands)
        {
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
        }
    }

    public void OnToggleGloves(EntityUid uid, MagneticGlovesComponent component, ToggleMagneticGlovesEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ToggleGloves(uid, component);
    }

    public void ToggleGloves(EntityUid uid, MagneticGlovesComponent component)
    {
        component.Enabled = !component.Enabled;
        _sharedActions.SetToggled(component.ToggleActionEntity, component.Enabled);

        if (_sharedContainer.TryGetContainingContainer(uid, out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, "gloves", out var entityUid) && entityUid == uid)
        {
            if (component.Enabled)
            {
                EnsureComp<KeepItemsOnFallComponent>(container.Owner);
                component.Debugger = "Enabled";
            }
            else
            {
                RemComp<KeepItemsOnFallComponent>(container.Owner);
                component.Debugger = "Disabled";
            }
        }
    }
}

public sealed partial class ToggleMagneticGlovesEvent : InstantActionEvent {}
