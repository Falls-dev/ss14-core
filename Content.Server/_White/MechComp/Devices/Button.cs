using Content.Shared._White.MechComp;
using Content.Shared.Interaction;
using Robust.Shared.Audio;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
    private void InitButton()
    {
        SubscribeLocalEvent<MechCompButtonComponent, ComponentInit>(OnButtonInit);
        SubscribeLocalEvent<MechCompButtonComponent, MechCompConfigAttemptEvent>(OnButtonConfigAttempt);
        SubscribeLocalEvent<MechCompButtonComponent, MechCompConfigUpdateEvent>(OnButtonConfigUpdate);
        SubscribeLocalEvent<MechCompButtonComponent, InteractHandEvent>(OnButtonHandInteract);
        SubscribeLocalEvent<MechCompButtonComponent, ActivateInWorldEvent>(OnButtonActivation);
    }

    private void OnButtonInit(EntityUid uid, MechCompButtonComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, "MechCompStandardOutput");
    }

    private void OnButtonConfigAttempt(EntityUid uid, MechCompButtonComponent comp, MechCompConfigAttemptEvent args)
    {
        args.entries.Add((typeof(string), "Сигнал на выходе", comp.outSignal));
    }
    private void OnButtonConfigUpdate(EntityUid uid, MechCompButtonComponent comp, MechCompConfigUpdateEvent args)
    {
        comp.outSignal = (string) args.results[0];
    }
    private void OnButtonHandInteract(EntityUid uid, MechCompButtonComponent comp, InteractHandEvent args)
    {
        ButtonClick(uid, comp);
    }
    private void OnButtonActivation(EntityUid uid, MechCompButtonComponent comp, ActivateInWorldEvent args)
    {
        ButtonClick(uid, comp);
    }
    private void ButtonClick(EntityUid uid, MechCompButtonComponent comp)
    {
        if (isAnchored(uid) && Cooldown(uid, "pressed", 1f))
        {
            _audio.PlayPvs(comp.ClickSound, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
            SendMechCompSignal(uid, "MechCompStandardOutput", comp.outSignal);
            ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated"); // the data will be discarded anyways
        }
    }

}
