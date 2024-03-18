using Content.Shared._White.MechComp;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.StepTrigger.Systems;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
    private void InitPressurePad()
    {
        SubscribeLocalEvent<MechCompPressurePadComponent, ComponentInit>(OnPressurePadInit);
        SubscribeLocalEvent<MechCompPressurePadComponent, StepTriggerAttemptEvent>(OnPressurePadStepAttempt);
        SubscribeLocalEvent<MechCompPressurePadComponent, StepTriggeredEvent>(OnPressurePadStep);
    }

	public void OnPressurePadInit(EntityUid uid, MechCompPressurePadComponent comp, ComponentInit args)
    {
        EnsureConfig(uid).Build(
            ("triggered_by_mobs", (typeof(bool), "Реагировать на существ", true) ),
            ("triggered_by_items", (typeof(bool), "Реагировать на предметы", false))
            );
        _link.EnsureSourcePorts(uid, "MechCompStandardOutput");
    }

    private void OnPressurePadStepAttempt(EntityUid uid, MechCompPressurePadComponent component, StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    public void OnPressurePadStep(EntityUid uid, MechCompPressurePadComponent comp, ref StepTriggeredEvent args)
    {
        if (HasComp<MobStateComponent>(args.Tripper) && GetConfig(uid).GetBool("triggered_by_mobs"))
        {
            SendMechCompSignal(uid, "MechCompStandardOutput", Comp<MetaDataComponent>(args.Tripper).EntityName);
            return;
			}
        if (HasComp<ItemComponent>(args.Tripper) && GetConfig(uid).GetBool("triggered_by_items"))
        {
            SendMechCompSignal(uid, "MechCompStandardOutput", Comp<MetaDataComponent>(args.Tripper).EntityName);
            return;
        }
    }

}
