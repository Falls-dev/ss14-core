using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

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
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MagneticGlovesComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MagneticGlovesComponent, ToggleMagneticGlovesEvent>(OnToggleGloves);
        SubscribeLocalEvent<MagneticGlovesComponent, DeactivateMagneticGlovesEvent>(DeactivateGloves);
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

        ActivateGloves(uid, component);
    }

    public void ActivateGloves(EntityUid uid, MagneticGlovesComponent component)
    {
        _sharedContainer.TryGetContainingContainer(uid, out var container);
        if (component.GlovesReadyAt.CompareTo(_gameTiming.CurTime) == 1)
        {
            if (container != null)
            {
                _popup.PopupEntity(Loc.GetString("maggloves-not-ready"), uid, container.Owner);
            }
            _sharedActions.SetToggled(component.ToggleActionEntity, component.Enabled);
            return;
        }

        component.Enabled = true;
        component.GlovesLastActivation = _gameTiming.CurTime;
        _sharedActions.SetToggled(component.ToggleActionEntity, component.Enabled);

        if (container != null)
        {
            EnsureComp<KeepItemsOnFallComponent>(container.Owner);
            if (TryComp<MagneticGlovesAdvancedComponent>(uid, out var adv))
            {
                EnsureComp<PreventDisarmComponent>(container.Owner);
                EnsureComp<PreventStrippingFromHandsAndGlovesComponent>(container.Owner);
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

    public void DeactivateGloves(EntityUid uid, MagneticGlovesComponent component, DeactivateMagneticGlovesEvent args)
    {
        component.Enabled = false;
        _sharedActions.SetToggled(component.ToggleActionEntity, component.Enabled);
        component.GlovesReadyAt = _gameTiming.CurTime + component.GlovesCooldown;
        if (_sharedContainer.TryGetContainingContainer(uid, out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, "gloves", out var entityUid))
        {
            RemComp<KeepItemsOnFallComponent>(container.Owner);
            if (TryComp<MagneticGlovesAdvancedComponent>(uid, out var adv))
            {
                RemComp<PreventDisarmComponent>(container.Owner);
                RemComp<PreventStrippingFromHandsAndGlovesComponent>(container.Owner);
            }
        }
        if (TryComp<AppearanceComponent>(uid, out var appearance) &&
            TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, component.Enabled ? "on" : "off", false, item);
            _appearance.SetData(uid, ToggleVisuals.Toggled, component.Enabled, appearance);
            _clothing.SetEquippedPrefix(uid, component.Enabled ? "on" : null);
        }
        args.Handled = true;
    }

}

public sealed partial class ToggleMagneticGlovesEvent : InstantActionEvent {}

public sealed partial class DeactivateMagneticGlovesEvent : InstantActionEvent {}
