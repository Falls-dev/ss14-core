using Content.Shared._White.StationaryTurret;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;

namespace Content.Server._White.StationaryTurret;

public sealed partial class StationaryTurret : SharedStationaryTurretSystem
{

    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationaryTurretComponent, TurretEntryEvent>(OnTurretEntry);
        SubscribeLocalEvent<StationaryTurretComponent, TurretExitEvent>(OnTurretExit);
        SubscribeLocalEvent<StationaryTurretComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
    }

    private void OnTurretEntry(EntityUid uid, StationaryTurretComponent component, TurretEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (component.PilotWhitelist != null && !component.PilotWhitelist.IsValid(args.User))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", uid)), args.User);
            return;
        }

        TryInsert(uid, args.Args.User, component);
        args.Handled = true;

    }

    private void OnTurretExit(EntityUid uid, StationaryTurretComponent component, TurretExitEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        TryEject(uid, component);
        args.Handled = true;
    }

    private void OnAlternativeVerb(EntityUid uid, StationaryTurretComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, 1, new TurretEntryEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };

            args.Verbs.Add(enterVerb);
        }
        else if (!IsEmpty(component))
        {
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1,
                Act = () =>
                {
                    if (args.User == uid || args.User == component.Pilot)
                    {
                        TryEject(uid, component);
                        return;
                    }

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, 0,
                        new TurretExitEvent(), uid, target: uid);

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new ValueList<(EntityUid, StationaryTurretPilotComponent)>();
        var query = EntityQueryEnumerator<StationaryTurretPilotComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<MobStateComponent>(uid, out var state))
                continue;
            if (state.CurrentState is MobState.Dead or MobState.Critical)
                toRemove.Add((uid, comp));
        }

        foreach (var (uid, comp) in toRemove)
        {
            TryEjectUser(uid);
        }
    }

}
