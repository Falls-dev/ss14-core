using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Timing;

namespace Content.Server._White.Chemistry
{
    public sealed class AutoRegenReagentSystem : EntitySystem
    {
        [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
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
            if (_solutionSystem.TryGetSolution(uid, component.SolutionName, out var _, out var solution))
                component.Generated = solution;

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


        private void SwitchReagent(AutoRegenReagentComponent component, EntityUid user)
        {
            if (component.CurrentIndex + 1 == component.Reagents.Count)
                component.CurrentIndex = 0;
            else
                component.CurrentIndex++;

            if (component.Generated != null)
                component.Generated.ScaleSolution(0);

            component.CurrentReagent = component.Reagents[component.CurrentIndex];

            _popups.PopupEntity(Loc.GetString("autoregen-switched", ("reagent", component.CurrentReagent)), user, user);
        }

        private void OnExamined(EntityUid uid, AutoRegenReagentComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("reagent-name", ("reagent", component.CurrentReagent)));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<AutoRegenReagentComponent, SolutionContainerManagerComponent>();
            while (query.MoveNext(out var uid, out var regen, out var manager))
            {
                if (_timing.CurTime < regen.NextRegenTime)
                    continue;

                regen.NextRegenTime = _timing.CurTime + regen.Duration;

                if (!_solutionSystem.ResolveSolution((uid, manager), regen.SolutionName, ref regen.Solution, out var solution))
                    continue;

                if (regen.Generated == null)
                    continue;

                var amount = FixedPoint2.Min(solution.AvailableVolume, regen.Generated.Volume);
                if (amount <= FixedPoint2.Zero)
                    continue;

                var generated = amount == regen.Generated.Volume ? regen.Generated : regen.Generated.Clone().SplitSolution(amount);

                _solutionSystem.TryAddSolution(regen.Solution.Value, generated);
            }
        }
    }
}
