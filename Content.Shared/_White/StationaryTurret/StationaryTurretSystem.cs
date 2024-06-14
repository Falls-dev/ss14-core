using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._White.StationaryTurret;


public abstract class SharedStationaryTurretSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContentEyeSystem _eyeSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StationaryTurretComponent, InteractNoHandEvent>(RelayInteractionEvent);
        SubscribeLocalEvent<StationaryTurretComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<StationaryTurretComponent, TurretEjectPilotEvent>(OnEjectPilotEvent);

        SubscribeLocalEvent<StationaryTurretPilotComponent, UpdateCanMoveEvent>(CanMove);
        SubscribeLocalEvent<StationaryTurretPilotComponent, AttackAttemptEvent>(CanAttack);
    }

    private void OnEjectPilotEvent(EntityUid uid, StationaryTurretComponent component, TurretEjectPilotEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        TryEject(uid, component);
    }

    private void RelayInteractionEvent(EntityUid uid, StationaryTurretComponent component, InteractNoHandEvent args)
    {
        var user = component.Pilot;
        if (!_actionBlocker.CanInteract(user, uid))
            return;

        if(!TryComp<AutoShootGunComponent>(uid, out var auto))
            return;

        if(auto.Enabled == false)
            _gunSystem.SetEnabled(uid, auto, true);
        else
            _gunSystem.SetEnabled(uid, auto, false);

    }

    private void OnDestruction(EntityUid uid, StationaryTurretComponent component, DestructionEventArgs args)
    {
        BreakTurret(uid, component);
    }

    private void SetupUser(EntityUid turret, EntityUid pilot, StationaryTurretComponent? component = null)
    {
        if (!Resolve(turret, ref component))
            return;

        if(!CanInteract(pilot, turret))
            return;

        var rider = EnsureComp<StationaryTurretPilotComponent>(pilot);
        var irelay = EnsureComp<InteractionRelayComponent>(pilot);

        component.Pilot = pilot;
        rider.Turret = turret;

        _interaction.SetRelay(pilot, turret, irelay);
        _actionBlocker.UpdateCanMove(pilot);
        _eyeSystem.SetZoom(pilot, component.Zoom, ignoreLimits: true);
        Dirty(pilot, rider);

        if (_net.IsClient)
            return;

        _actions.AddAction(pilot, ref component.TurretEjectActionEntity, component.TurretEjectAction, turret);
    }

    public void RemoveUser(EntityUid turret, EntityUid pilot)
    {
        _actions.RemoveProvidedActions(pilot, turret);

        if (!RemComp<StationaryTurretPilotComponent>(pilot))
            return;
        RemComp<RelayInputMoverComponent>(pilot);
        RemComp<InteractionRelayComponent>(pilot);

        if (TryComp<AutoShootGunComponent>(turret, out var auto))
          _gunSystem.SetEnabled(turret, auto, false);
        if (TryComp<StationaryTurretComponent>(turret, out var comp))
            comp.Pilot = new EntityUid();
        _actionBlocker.UpdateCanMove(pilot);
        _eyeSystem.ResetZoom(pilot);

    }

    private void BreakTurret(EntityUid uid, StationaryTurretComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TryEject(uid, component);
    }

    protected bool IsEmpty(StationaryTurretComponent component)
    {
        return !component.Pilot.Valid;
    }

    protected bool CanInsert(EntityUid uid, EntityUid toInsert, StationaryTurretComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return IsEmpty(component) && _actionBlocker.CanMove(toInsert);
    }


    protected bool TryInsert(EntityUid uid, EntityUid toInsert, StationaryTurretComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        SetupUser(uid, toInsert);
        return true;
    }

    protected void TryEjectUser(EntityUid uid, StationaryTurretPilotComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        var turret = component.Turret;

        RemoveUser(turret, uid);
    }

    protected void TryEject(EntityUid uid, StationaryTurretComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var pilot = component.Pilot;
        RemoveUser(uid, pilot);
    }

    private void CanAttack(EntityUid uid, StationaryTurretPilotComponent component, CancellableEntityEventArgs args)
    {
        if(component.Turret.Valid)
            args.Cancel();
    }

    private void CanMove(EntityUid uid, StationaryTurretPilotComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private bool CanInteract(EntityUid user, EntityUid uid)
    {
        if (!TryComp<StationaryTurretComponent>(uid, out var component)
            || !Transform(uid).Anchored
            || !_actionBlocker.CanInteract(user, uid))
            return false;

        return true;
    }

}

[Serializable, NetSerializable]
public sealed partial class TurretExitEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class TurretEntryEvent : SimpleDoAfterEvent
{
}

public sealed partial class TurretEjectPilotEvent : InstantActionEvent
{
}

