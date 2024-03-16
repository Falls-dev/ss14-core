using Content.Client.Chemistry.Components;
using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class InjectorSystem : SharedInjectorSystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<InjectorComponent>(ent => new InjectorStatusControl(ent, SolutionContainers));
        SubscribeLocalEvent<HyposprayComponent, ComponentHandleState>(OnHandleHyposprayState);
        Subs.ItemStatus<HyposprayComponent>(ent => new HyposprayStatusControl(ent));
        //SubscribeLocalEvent<PatchComponent, ComponentHandleState>(OnHandlePatchState);
    }

    private void OnHandleHyposprayState(EntityUid uid, HyposprayComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HyposprayComponentState cState)
            return;

        component.CurrentVolume = cState.CurVolume;
        component.TotalVolume = cState.MaxVolume;
        component.UiUpdateNeeded = true;
    }

    /*private void OnHandlePatchState(EntityUid uid, PatchComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PatchComponentState cState)
            return;

        component.CurrentVolume = cState.CurVolume;
        component.TotalVolume = cState.MaxVolume;
    }*/
}
