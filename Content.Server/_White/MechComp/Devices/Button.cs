using Content.Shared._White.MechComp;
using Content.Shared.Interaction;
using Robust.Shared.Audio;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
    private void InitButton()
    {
        SubscribeLocalEvent<MechCompButtonComponent, ComponentInit>(OnButtonInit);
        SubscribeLocalEvent<MechCompButtonComponent, InteractHandEvent>(OnButtonHandInteract);
        SubscribeLocalEvent<MechCompButtonComponent, ActivateInWorldEvent>(OnButtonActivation);

    }

    private void OnButtonInit(EntityUid uid, MechCompButtonComponent comp, ComponentInit args)
    {
        EnsureConfig(uid).Build(
            ("outsignal", (typeof(string), "Сигнал на выходе", "1"))
            );
        _link.EnsureSourcePorts(uid, "MechCompStandardOutput");

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
            SendMechCompSignal(uid, "MechCompStandardOutput", GetConfigString(uid, "outsignal"));
            ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated"); // the data will be discarded anyways
        }
    }

}
