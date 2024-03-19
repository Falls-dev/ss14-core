using Content.Server._White.TTS;
using Content.Server.Chat.Systems;
//using Content.Shared.Administration;
using Content.Server.DeviceLinking.Events;
using Content.Server.VoiceMask;
using Content.Shared._White.MechComp;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
    private void InitSpeaker()
    {
        SubscribeLocalEvent<MechCompSpeakerComponent, ComponentInit>(OnSpeakerInit);
        SubscribeLocalEvent<MechCompSpeakerComponent, MechCompConfigAttemptEvent>(OnSpeakerConfigAttempt);
        SubscribeLocalEvent<MechCompSpeakerComponent, MechCompConfigUpdateEvent>(OnSpeakerConfigUpdate);
        SubscribeLocalEvent<MechCompSpeakerComponent, SignalReceivedEvent>(OnSpeakerSignal);
        SubscribeLocalEvent<MechCompSpeakerComponent, TransformSpeakerVoiceEvent>(OnSpeakerVoiceTransform);
    }


    private void OnSpeakerInit(EntityUid uid, MechCompSpeakerComponent comp, ComponentInit args)
    {
        if (comp.name == "")
            comp.name = Name(uid);
        _link.EnsureSinkPorts(uid, "MechCompStandardInput");
    }

    private void OnSpeakerConfigAttempt(EntityUid uid, MechCompSpeakerComponent comp, MechCompConfigAttemptEvent args)
    {
        args.entries.Add((typeof(bool), "Голосить в радио (;)", comp.inRadio));
        args.entries.Add((typeof(string), "Имя", comp.name));
    }
    private void OnSpeakerConfigUpdate(EntityUid uid, MechCompSpeakerComponent comp, MechCompConfigUpdateEvent args)
    {
        comp.inRadio = (bool) args.results[0];
        comp.name = (string) args.results[1];
    }
    private void OnSpeakerSignal(EntityUid uid, MechCompSpeakerComponent comp, ref SignalReceivedEvent args)
    {
        if (isAnchored(uid) && TryGetMechCompSignal(args.Data, out string msg))
        {
            msg = msg.ToUpper();

            if (comp.inRadio && Cooldown(uid, "speech", 5f)) // higher cooldown if we're speaking in radio
            {
                ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated");
                _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true, checkRadioPrefix: false, nameOverride: comp.name);
                _radio.SendRadioMessage(uid, msg, "Common", uid);
            }
            else if (Cooldown(uid, "speech", 1f))
            {
                ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated");
                _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true, checkRadioPrefix: false, nameOverride: comp.name);
            }
        }
    }

    private void OnSpeakerVoiceTransform(EntityUid uid, MechCompSpeakerComponent comp, TransformSpeakerVoiceEvent args)
    {
        args.VoiceId = comp.name;
    }
}
