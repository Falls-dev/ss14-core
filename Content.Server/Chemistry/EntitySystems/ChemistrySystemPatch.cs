using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameStates;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry;
using Content.Shared.DoAfter;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed partial class ChemistrySystem
    {
        [Dependency] private readonly ReactiveSystem _reactive = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

        public void InitializePatch()
        {
            SubscribeLocalEvent<PatchComponent, PatchDoAfterEvent>(OnPatchDoAfter);
            SubscribeLocalEvent<PatchComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<PatchComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PatchComponent, ComponentGetState>(OnPatchGetState);
            SubscribeLocalEvent<PatchComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        }

        private void OnPatchGetState(Entity<PatchComponent> entity, ref ComponentGetState args)
        {
            args.State = _solutionContainers.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out _, out var solution)
                ? new PatchComponentState(solution.Volume, solution.MaxVolume)
                : new PatchComponentState(FixedPoint2.Zero, FixedPoint2.Zero);
        }

        // fucking doafter fucking doafter fucking doafterfucking doafterfucking doafterfucking doafterfucking doafterfucking doafterfucking doafter
        // i ll trigger method which ll trigger event which ll trigger method which ll trigger big fat cock in hairy fat ass wizden dev
        private void OnPatchDoAfter(Entity<PatchComponent> entity, ref PatchDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            TryDoInject(entity, args.Args.Target.Value, args.Args.User);
            args.Handled = true;
        }

        private void PatchDoAfter(Entity<PatchComponent> patch, EntityUid target, EntityUid user)
        {
            // Create a pop-up for the user
            _popup.PopupEntity(Loc.GetString("patch-component-injecting-user", ("target", target)), target, user);

            // Create a pop-up for the target
            _popup.PopupEntity(Loc.GetString("patch-component-target-getting-injected"), target, target);

            var actualDelay = MathHelper.Max(patch.Comp.Delay, TimeSpan.FromSeconds(1));

            _adminLogger.Add(LogType.ForceFeed, $"{_entMan.ToPrettyString(user):user} is attempting to put a patch on {_entMan.ToPrettyString(target):target}");

            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, actualDelay, new PatchDoAfterEvent(), patch.Owner, target: target, used: patch.Owner)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true
            });
        }

        /// <summary>
        /// Actually difference between OnUseInHand and OnAfterInteract only in target
        /// In OnUseInHand target is always = user. In OnAfterInteract target may be user or may not
        /// </summary>
        private void OnUseInHand(Entity<PatchComponent> entity, ref UseInHandEvent args)
        {
            if (args.Handled)
                return;

            if (args.User is not { Valid: true } target)
                return;

            PatchDoAfter(entity, target, args.User);

            args.Handled = true;
        }

        private void OnAfterInteract(Entity<PatchComponent> entity, ref AfterInteractEvent args)
        {
            if (!args.CanReach || args.Handled)
                return;

            var (_, component) = entity;

            if (!EligibleEntity(args.Target, _entMan, component))
                return;

            if (args.Target is not { Valid: true } target)
                return;

            var user = args.User;

            PatchDoAfter(entity, target, user);
            args.Handled = true;
        }

        private void OnSolutionChange(Entity<PatchComponent> entity, ref SolutionContainerChangedEvent args)
        {
            Dirty(entity);
        }

        public bool TryDoInject(Entity<PatchComponent> patch, EntityUid? target, EntityUid user)
        {
            var (uid, component) = patch;

            string? msgFormat = null;
            if (!EligibleEntity(target, _entMan, component))
                return false;

            if (!_solutionContainers.TryGetSolution(uid, component.SolutionName, out var patchSoln, out var patchSolution) || patchSolution.Volume == 0)
            {
                // TODO: Empty patch should stop the bleeding

                _popup.PopupCursor(Loc.GetString("patch-component-empty-message"), user);
                return true;
            }

            if (!_solutionContainers.TryGetInjectableSolution(target.Value, out var targetSoln, out var targetSolution))
            {
                _popup.PopupCursor(Loc.GetString("patch-cant-inject", ("target", Identity.Entity(target.Value, _entMan))), user);
                return false;
            }

            if (patchSolution.Volume > targetSolution.AvailableVolume)
            {
                _popup.PopupCursor(Loc.GetString("patch-cant-inject-now"), user);
                return false;
            }

            var removedSolution = _solutionContainers.SplitSolution(patchSoln.Value, patchSolution.Volume);

            _popup.PopupCursor(Loc.GetString(msgFormat ?? "patch-component-inject-other-message", ("other", target)), user);

            if (!targetSolution.CanAddSolution(removedSolution))
                return true;

            _reactive.DoEntityReaction(target.Value, removedSolution, ReactionMethod.Touch);
            _reactive.DoEntityReaction(target.Value, removedSolution, ReactionMethod.Injection);
            _solutionContainers.TryAddSolution(targetSoln.Value, removedSolution);
            QueueDel(patch);

            _adminLogger.Add(LogType.ForceFeed, $"{_entMan.ToPrettyString(user):user} put a patch on {_entMan.ToPrettyString(target.Value):target} with a solution {SolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {_entMan.ToPrettyString(uid):using}");

            return true;
        }

        static bool EligibleEntity([NotNullWhen(true)] EntityUid? entity, IEntityManager entMan, PatchComponent component)
        {
            // Using patch only on mobs
            return component.OnlyMobs
                ? entMan.HasComponent<SolutionContainerManagerComponent>(entity) &&
                  entMan.HasComponent<MobStateComponent>(entity)
                : entMan.HasComponent<SolutionContainerManagerComponent>(entity);
        }
    }
}
