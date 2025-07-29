using Content.Server._White.Genetics.Components;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._White.Genetics;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared._White.Genetics.Components;

namespace Content.Server._White.Genetics.Systems;

public sealed class GeneticInjectorSystem : SharedGeneticInjectorSystem
{
    [Dependency] private readonly GenomeSystem _genome = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticInjectorComponent, GeneticInjectorDoAfterEvent>(OnInjectDoAfterComplete);
        SubscribeLocalEvent<GeneticInjectorComponent, AfterInteractEvent>(OnAfterInteract);
    }


    // TODO: log
    public void OnAfterInteract(Entity<GeneticInjectorComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<GenomeComponent>(args.Target.Value, out var targetGenome))
        {
            if (!entity.Comp.Forced)
            {
                return;
            }

            targetGenome = new GenomeComponent();
            AddComp(args.Target.Value, targetGenome);
        }

        if (!entity.Comp.Used)
            return;

        if (entity.Comp.MutationProtos.Count + entity.Comp.ActivatorMutations.Count == 0)
            return;

        var delay = entity.Comp.UseDelay;

        if (args.User != args.Target.Value)
        {
            // Create a pop-up for the target
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                ("user", userName)), args.User, args.Target.Value);

            // Check if the target is incapacitated or in combat mode and modify time accordingly.
            if (_mobState.IsIncapacitated(args.Target.Value))
            {
                delay /= 2.5f;
            }
        }
        else
        {
            delay /= 2.5f;
        }

        // TODO: admin log here
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, delay, new GeneticInjectorDoAfterEvent(),
            entity.Owner, target: args.Target.Value, used: entity.Owner)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
        });
    }

    public void OnInjectDoAfterComplete(Entity<GeneticInjectorComponent> injector, ref GeneticInjectorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (!TryComp<GenomeComponent>(args.Target.Value, out var targetGenome))
            return;

        var mutationList = new List<string>();

        // mutator mutations
        foreach (var mutation in injector.Comp.MutationProtos)
        {
            _genome.ApplyMutatorMutation(args.Target.Value, targetGenome, mutation);
            mutationList.Add(mutation);
        }

        // activator mutations
        foreach (var mutation in injector.Comp.ActivatorMutations)
        {
            _genome.ApplyActivatorMutation(args.Target.Value, targetGenome, mutation);
            mutationList.Add(mutation);
        }

        injector.Comp.MutationProtos.Clear(); // TODO: probably need another way to make injectors single-use
        injector.Comp.ActivatorMutations.Clear();
        injector.Comp.Used = true;



        // TODO: admin log here, use mutationList
    }
}
