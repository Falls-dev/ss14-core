using Content.Server.Administration;
using Content.Server.DeviceLinking.Events;
using Content.Shared._White.MechComp;
using Content.Shared.Maps;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
    private void InitTeleport()
    {
        SubscribeLocalEvent<MechCompTeleportComponent, ComponentInit>(OnTeleportInit);
        SubscribeLocalEvent<MechCompTeleportComponent, MechCompConfigAttemptEvent>(OnTeleportConfigAttempt);
        SubscribeLocalEvent<MechCompTeleportComponent, MechCompConfigUpdateEvent>(OnTeleportConfigUpdate);
        SubscribeLocalEvent<MechCompTeleportComponent, SignalReceivedEvent>(OnTeleportSignal);
    }


    private void OnTeleportInit(EntityUid uid, MechCompTeleportComponent comp, ComponentInit args)
    {
        if (comp.teleId < 0 || comp.teleId > 65535)
            comp.teleId = _rng.Next(65536);
        _link.EnsureSinkPorts(uid, "MechCompTeleIDInput");
    }

    private void OnTeleportConfigAttempt(EntityUid uid, MechCompTeleportComponent comp, MechCompConfigAttemptEvent args)
    {
        args.entries.Add((typeof(Hex16), "ID этого телепорта", comp.teleId));
        args.entries.Add((null, "Установите ID на 0000, чтобы отключить приём."));
    }
    private void OnTeleportConfigUpdate(EntityUid uid, MechCompTeleportComponent comp, MechCompConfigUpdateEvent args)
    {
        comp.teleId = (int) args.results[0];
    }

    private void OnTeleportSignal(EntityUid uid, MechCompTeleportComponent comp, ref SignalReceivedEvent args)
    {
        if (IsOnCooldown(uid, "teleport"))
        {
            //_audio.PlayPvs("/Audio/White/MechComp/generic_energy_dryfire.ogg", uid);
            //return;
        }
        if (!TryGetMechCompSignal(args.Data, out string _sig) ||
            !int.TryParse(_sig, System.Globalization.NumberStyles.HexNumber, null, out int targetId) ||
            targetId == 0)
        {
            return;
        }


        var xform = Comp<TransformComponent>(uid);
        TransformComponent? target = null;
        if (!TryComp<TransformComponent>(uid, out var telexform)) return;
        foreach (var (othercomp, otherbase, otherxform) in EntityQuery<MechCompTeleportComponent, BaseMechCompComponent, TransformComponent>())
        {
            var otherUid = othercomp.Owner;
            var distance = (_xform.GetWorldPosition(uid) - _xform.GetWorldPosition(otherUid)).Length();
            if (otherxform.Anchored && targetId == othercomp.teleId)
            {
                if (distance <= comp.MaxDistance && distance <= othercomp.MaxDistance && xform.MapID == otherxform.MapID) // huh
                {
                    target = otherxform;
                    break;
                }
            }
        }
        if (target == null)
        {
            _audio.PlayPvs("/Audio/White/MechComp/generic_energy_dryfire.ogg", uid);
            Cooldown(uid, "teleport", 0.7f);
            return;
        }

        var targetUid = target.Owner;
        _appearance.SetData(uid, MechCompDeviceVisuals.Mode, "firing");
        _appearance.SetData(targetUid, MechCompDeviceVisuals.Mode, "charging");

        // because the target tele has a cooldown of a second, it can be used to quickly move
        // back and make the original tele and reset it's cooldown down to a second.
        // i decided it would be fun to abuse, and thus, it will be left as is
        // if it turns out to be not fun, add check that newCooldown > currentCooldown
        ForceCooldown(uid, "teleport", 7f, () => { _appearance.SetData(uid, MechCompDeviceVisuals.Mode, "ready"); });
        ForceCooldown(targetUid, "teleport", 1f, () => { _appearance.SetData(target.Owner, MechCompDeviceVisuals.Mode, "ready"); });

        Spawn("EffectSparks", Transform(uid).Coordinates);
        Spawn("EffectSparks", Transform(targetUid).Coordinates);
        _audio.PlayPvs("/Audio/White/MechComp/emitter2.ogg", uid);
        _audio.PlayPvs("/Audio/White/MechComp/emitter2.ogg", targetUid);
        // var sol = new Solution();
        // sol.AddReagent("Water", 500f); // hue hue hue
        // _smoke.StartSmoke(uid, sol, 6f, 1);
        // sol = new Solution();
        // sol.AddReagent("Water", 500f); // hue hue hue
        // _smoke.StartSmoke(uid, sol, 6f, 1);

        foreach (EntityUid u in TurfHelpers.GetEntitiesInTile(telexform.Coordinates, LookupFlags.Uncontained))
        {
            if (TryComp<TransformComponent>(u, out var uxform) && !uxform.Anchored)
            {
                _xform.SetCoordinates(u, target.Coordinates);
            }
        }
    }
}
