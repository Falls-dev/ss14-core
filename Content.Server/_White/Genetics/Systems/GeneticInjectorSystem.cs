using Content.Server._White.Genetics.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Pidgin.Configuration;

namespace Content.Server._White.Genetics.Systems;

public sealed class GeneticInjectorSystem : EntitySystem
{
    [Dependency] private readonly GenomeSystem _genome = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticInjectorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public void OnAfterInteract(Entity<GeneticInjectorComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target == null)
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

        // mutator mutations
        foreach (var mutation in entity.Comp.MutationProtos)
        {
            _genome.ApplyMutatorMutation(args.Target.Value, targetGenome, mutation);
        }

        // activator mutations

    }
}
