using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared._White.MagGloves;
using Robust.Shared.Containers;

namespace Content.Server._White.MagGloves;

/// <summary>
/// This handles...
/// </summary>
public sealed class MagneticGlovesSystem : EntitySystem
{

    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MagneticGlovesComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagneticGlovesComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MagneticGlovesComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MagneticGlovesComponent>();
        while (query.MoveNext(out var uid, out var gloves))
        {

            if(!TryComp<BatteryComponent>(uid, out var batComp))
                continue;

            if (!TryComp(uid, out MagneticGlovesComponent? magcomp))
                continue;

            if (!magcomp.Enabled)
                return;

            if (!batComp.TryUseCharge(gloves.Wattage * frameTime))
            {
                if (_sharedContainer.TryGetContainingContainer(uid, out var container))
                {
                    ToggleGloves(container.Owner, magcomp, false);
                    RaiseLocalEvent(uid, new ToggleMagneticGlovesEvent());
                }
            }
        }
    }

    public void OnGotUnequipped(EntityUid uid, MagneticGlovesComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "gloves")
        {
            ToggleGloves(args.Equipee, component, false);
        }
    }

    public void OnGotEquipped(EntityUid uid, MagneticGlovesComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "gloves")
        {
            ToggleGloves(args.Equipee, component, true);
        }
    }

    public void ToggleGloves(EntityUid owner, MagneticGlovesComponent component, bool active)
    {
        if (!active)
        {
            RemComp<KeepItemsOnFallComponent>(owner);
            if (TryComp<MagneticGlovesAdvancedComponent>(owner, out var adv))
            {
                RemComp<PreventDisarmComponent>(owner);
                RemComp<PreventStrippingFromHandsAndGlovesComponent>(owner);
            }
        }
        else if (component.Enabled)
        {
            EnsureComp<KeepItemsOnFallComponent>(owner);
            if (TryComp<MagneticGlovesAdvancedComponent>(owner, out var adv))
            {
                EnsureComp<PreventDisarmComponent>(owner);
                EnsureComp<PreventStrippingFromHandsAndGlovesComponent>(owner);
            }
        }
    }

    public void OnExamined(EntityUid uid, MagneticGlovesComponent component, ExaminedEvent args)
    {
        if (!TryComp(uid, out BatteryComponent? battery))
            return;

        if (!args.IsInDetailsRange)
            return;

        var message = Loc.GetString("maggloves-charge") + " " + Math.Round(battery.CurrentCharge / battery._maxCharge * 100) + "%";
        args.PushMarkup(message);
    }
}
