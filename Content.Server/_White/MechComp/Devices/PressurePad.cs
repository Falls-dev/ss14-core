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
        SubscribeLocalEvent<MechCompPressurePadComponent, MechCompConfigAttemptEvent>(OnPressurePadConfigAttempt);
        SubscribeLocalEvent<MechCompPressurePadComponent, MechCompConfigUpdateEvent>(OnPressurePadConfigUpdate);
        SubscribeLocalEvent<MechCompPressurePadComponent, StepTriggerAttemptEvent>(OnPressurePadStepAttempt);
        SubscribeLocalEvent<MechCompPressurePadComponent, StepTriggeredEvent>(OnPressurePadStep);
    }

	public void OnPressurePadInit(EntityUid uid, MechCompPressurePadComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, "MechCompStandardOutput");
    }
    public void OnPressurePadConfigAttempt(EntityUid uid, MechCompPressurePadComponent comp, MechCompConfigAttemptEvent args)
    {
        args.entries.Add((typeof(bool), "Реагировать на существ", comp.reactToMobs));
        args.entries.Add((typeof(bool), "Реагировать на предметы", comp.reactToItems));
    }
    public void OnPressurePadConfigUpdate(EntityUid uid, MechCompPressurePadComponent comp, MechCompConfigUpdateEvent args)
    {
        comp.reactToMobs = (bool) args.results[0];
        comp.reactToItems = (bool) args.results[1];
    }

    private void OnPressurePadStepAttempt(EntityUid uid, MechCompPressurePadComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    public void OnPressurePadStep(EntityUid uid, MechCompPressurePadComponent comp, ref StepTriggeredEvent args)
    {
        if (HasComp<MobStateComponent>(args.Tripper) && comp.reactToMobs)
        {
            SendMechCompSignal(uid, "MechCompStandardOutput", Comp<MetaDataComponent>(args.Tripper).EntityName);
            return;
			}
        if (HasComp<ItemComponent>(args.Tripper) && comp.reactToItems)
        {
            SendMechCompSignal(uid, "MechCompStandardOutput", Comp<MetaDataComponent>(args.Tripper).EntityName);
            return;
        }
    }

}
