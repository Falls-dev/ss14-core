using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;

namespace Content.Shared._White.MagGloves;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedMagneticGlovesSystem : EntitySystem

{
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
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
                if (TryComp<MagneticGlovesAdvancedComponent>(uid, out var adv))
                {
                    EnsureComp<PreventDisarmComponent>(container.Owner);
                    EnsureComp<PreventStrippingFromHandsAndGlovesComponent>(container.Owner);
                    component.Debugger = "Enabled";
                }
            }
            else
            {
                RemComp<KeepItemsOnFallComponent>(container.Owner);
                if (TryComp<MagneticGlovesAdvancedComponent>(uid, out var adv))
                {
                    RemComp<PreventDisarmComponent>(container.Owner);
                    RemComp<PreventStrippingFromHandsAndGlovesComponent>(container.Owner);
                    component.Debugger = "Disabled";
                }
            }
        }

        if (TryComp<AppearanceComponent>(uid, out var appearance) &&
            TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, component.Enabled ? "on" : "off", false, item);
            _appearance.SetData(uid, ToggleVisuals.Toggled, component.Enabled, appearance);
            _clothing.SetEquippedPrefix(uid, component.Enabled ? "on" : null);
        }
    }
}

public sealed partial class ToggleMagneticGlovesEvent : InstantActionEvent {}
