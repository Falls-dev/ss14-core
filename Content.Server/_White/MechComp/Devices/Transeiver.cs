using Content.Server.Administration;
using Content.Server.DeviceLinking.Events;
using Content.Shared._White.MechComp;
using Content.Shared.DeviceNetwork;
using Content.Shared.Interaction;
using Robust.Shared.Audio;


namespace Content.Server._White.MechComp;

public readonly record struct MechCompWirelessTransmissionReceivedEvent(int targetId, NetworkPayload? Data);

public sealed partial class MechCompDeviceSystem
{
    private void InitTranseiver()
    {
        SubscribeLocalEvent<MechCompTranseiverComponent, ComponentInit>(OnTranseiverInit);
        SubscribeLocalEvent<MechCompTranseiverComponent, MechCompConfigAttemptEvent>(OnTranseiverConfigAttempt);
        SubscribeLocalEvent<MechCompTranseiverComponent, MechCompConfigUpdateEvent>(OnTranseiverConfigUpdate);
        SubscribeLocalEvent<MechCompTranseiverComponent, SignalReceivedEvent>(OnTranseiverSignalReceived);
        SubscribeLocalEvent<MechCompTranseiverComponent, MechCompWirelessTransmissionReceivedEvent>(OnTranseiverTransmissionReceived);
    }

    private void OnTranseiverInit(EntityUid uid, MechCompTranseiverComponent comp, ComponentInit args)
    {
        if (comp.thisId < 0 || comp.thisId > 65535)
            comp.thisId = _rng.Next(65536);
        _link.EnsureSourcePorts(uid, "MechCompStandardOutput");
        _link.EnsureSinkPorts(uid, "MechCompStandardInput");
    }

    private void OnTranseiverConfigAttempt(EntityUid uid, MechCompTranseiverComponent comp, MechCompConfigAttemptEvent args)
    {
        args.entries.Add((typeof(Hex16), "Код этого передатчика", comp.thisId));
        args.entries.Add((typeof(Hex16), "Код целевого передатчика", comp.targetId));
    }
    private void OnTranseiverConfigUpdate(EntityUid uid, MechCompTranseiverComponent comp, MechCompConfigUpdateEvent args)
    {   
        comp.thisId = (int) args.results[0];
        comp.targetId = (int) args.results[1];
    }
    private void OnTranseiverSignalReceived(EntityUid uid, MechCompTranseiverComponent comp, ref SignalReceivedEvent args)
    {
        ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated");
        foreach (var (othercomp, otherxform) in EntityQuery<MechCompTranseiverComponent, TransformComponent>())
        {
            if(othercomp.thisId == comp.targetId && otherxform.Anchored)
            {
                var otherUid = othercomp.Owner;
                //RaiseLocalEvent(new MechCompWirelessTransmissionReceivedEvent(comp.targetId, args.Data));
                ForceSetData(otherUid, MechCompDeviceVisuals.Mode, "activated");
                _link.InvokePort(otherUid, "MechCompStandardOutput", args.Data);
            }
        }
    }
    private void OnTranseiverTransmissionReceived(EntityUid uid, MechCompTranseiverComponent comp, ref MechCompWirelessTransmissionReceivedEvent args)
    {
        if (args.targetId == comp.thisId)
        {
            _link.InvokePort(uid, "MechCompStandardOutput", args.Data);
            ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated");
        }
    }
}
