using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Events;
using Content.Server.VoiceMask;
using Content.Shared._White.MechComp;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
    private void InitSpeaker()
    {

        SubscribeLocalEvent<MechCompSpeakerComponent, ComponentInit>(OnSpeakerInit);
        SubscribeLocalEvent<MechCompSpeakerComponent, MechCompConfigUpdateEvent>(OnSpeakerConfigUpdate);
        SubscribeLocalEvent<MechCompSpeakerComponent, SignalReceivedEvent>(OnSpeakerSignal);
    }


    private void OnSpeakerInit(EntityUid uid, MechCompSpeakerComponent comp, ComponentInit args)
    {
        _link.EnsureSinkPorts(uid, "MechCompStandardInput");

        EnsureConfig(uid).Build(
            ("inradio", (typeof(bool), "Голосить в радио (;)", false)),
            ("name", (typeof(string), "Имя", Name(uid)))
        );
        EnsureComp<VoiceMaskComponent>(uid, out var maskcomp);
        maskcomp.VoiceName = Name(uid); // better safe than █████ ███ ██████
    }
    private void OnSpeakerConfigUpdate(EntityUid uid, MechCompSpeakerComponent comp, MechCompConfigUpdateEvent args)
    {
        Comp<VoiceMaskComponent>(uid).VoiceName = GetConfigString(uid, "name");
    }
    private void OnSpeakerSignal(EntityUid uid, MechCompSpeakerComponent comp, ref SignalReceivedEvent args)
    {

        //Logger.Debug($"MechComp speaker received signal ({args.ToString()}) ({args.Data?.ToString()}) ({ToPrettyString(uid)})");
        if (isAnchored(uid) && TryGetMechCompSignal(args.Data, out string msg))
        {
            msg = msg.ToUpper();
            ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated");
            //Logger.Debug($"MechComp speaker spoke ({msg}) ({ToPrettyString(uid)})");
            if (GetConfigBool(uid, "inradio") && Cooldown(uid, "speech", 5f))
            {

                _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true, checkRadioPrefix: false, nameOverride: GetConfigString(uid, "name"));
                _radio.SendRadioMessage(uid, msg, "Common", uid);
            }
            else if (Cooldown(uid, "speech", 1f))
            {
                _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true, checkRadioPrefix: false, nameOverride: GetConfigString(uid, "name"));
            }
        }
    }


}
