using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Timing;

namespace Content.Server._White.AutoRegenReagent
{
    public sealed class AutoRegenReagentSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AutoRegenReagentComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<AutoRegenReagentComponent, GetVerbsEvent<AlternativeVerb>>(AddSwitchVerb);
            SubscribeLocalEvent<AutoRegenReagentComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<AutoRegenReagentComponent, UseInHandEvent>(OnUseInHand,
                before: new[] { typeof(ChemistrySystem) });
        }

        private void OnUseInHand(EntityUid uid, AutoRegenReagentComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            if (component.Reagents.Count <= 1)
                return;

            SwitchReagent(component, args.User);
            args.Handled = true;
        }

        private void OnInit(EntityUid uid, AutoRegenReagentComponent component, ComponentInit args)
        {
            if (component.SolutionName == null)
                return;
            if (_solutionSystem.TryGetSolution(uid, component.SolutionName, out var solution))
                component.Solution = solution;
            component.CurrentReagent = component.Reagents[component.CurrentIndex];
        }

        private void AddSwitchVerb(EntityUid uid, AutoRegenReagentComponent component,
            GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (component.Reagents.Count <= 1)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    SwitchReagent(component, args.User);
                },
                Text = Loc.GetString("autoreagent-switch"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }


        private string SwitchReagent(AutoRegenReagentComponent component, EntityUid user)
        {
            if (component.CurrentIndex + 1 == component.Reagents.Count)
                component.CurrentIndex = 0;
            else
                component.CurrentIndex++;

            if (component.Solution != null)
                _solutionSystem.RemoveAllSolution(component.Solution.Value);


            component.CurrentReagent = component.Reagents[component.CurrentIndex];

            _popups.PopupEntity(Loc.GetString("autoregen-switched", ("reagent", component.CurrentReagent)), user, user);

            return component.CurrentReagent;
        }

        private void OnExamined(EntityUid uid, AutoRegenReagentComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("reagent-name", ("reagent", component.CurrentReagent)));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var query = EntityQueryEnumerator<AutoRegenReagentComponent, SolutionComponent>();
            while (query.MoveNext(out var uid, out var autoComp, out _))
            {
                if (autoComp.Solution == null)
                    return;

                var time = _timing.CurTime;

                if (autoComp.NextUpdate >= time)
                    return;

                autoComp.NextUpdate = time + autoComp.Interval;

                _solutionSystem.TryAddReagent(autoComp.Solution.Value, autoComp.CurrentReagent, autoComp.UnitsPerInterval);
            }
        }
    }
}
