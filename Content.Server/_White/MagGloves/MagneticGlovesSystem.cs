using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared._White.MagGloves;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._White.MagGloves;

/// <summary>
/// This handles...
/// </summary>
public sealed class MagneticGlovesSystem : EntitySystem
{

    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MagneticGlovesComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagneticGlovesComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MagneticGlovesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MagneticGlovesComponent, EmpPulseEvent>(OnEmp);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MagneticGlovesComponent>();
        while (query.MoveNext(out var uid, out var gloves))
        {

            if (!TryComp(uid, out MagneticGlovesComponent? magcomp))
                continue;

            if (!magcomp.Enabled)
                return;

            if (_gameTiming.CurTime.CompareTo(magcomp.GlovesLastActivation.Add(magcomp.GlovesActiveTime)) == 1)
            {
                RaiseLocalEvent(uid, new DeactivateMagneticGlovesEvent());
            }

        }
    }

    public void OnEmp(EntityUid uid, MagneticGlovesComponent component, EmpPulseEvent args)
    {
        if (component.Enabled)
        {
            RaiseLocalEvent(uid, new DeactivateMagneticGlovesEvent());
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

        if (!args.IsInDetailsRange)
            return;

        var message = Loc.GetString("maggloves-ready-in") + " " + component.GlovesReadyAt.Subtract(_gameTiming.CurTime).TotalSeconds.ToString("0");

        if (component.GlovesReadyAt < _gameTiming.CurTime)
        {
            message = Loc.GetString("maggloves-ready");
        }

        if (component.Enabled)
        {
            message = Loc.GetString("maggloves-enabled-till") + " " + (component.GlovesLastActivation
                .Add(component.GlovesActiveTime).Subtract(_gameTiming.CurTime).TotalSeconds.ToString("0"));
        }

        args.PushMarkup(message);
    }
}
