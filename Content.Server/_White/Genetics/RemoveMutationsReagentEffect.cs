using System.Text.RegularExpressions;
using Content.Server._White.Genetics.Components;
using Content.Server._White.Genetics.Systems;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.Genetics
{
    [UsedImplicitly]
    public sealed partial class RemoveMutationsReagentEffect : ReagentEffect
    {

        // TODO: add probability
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent<GenomeComponent>(args.SolutionEntity, out var genome))
                return;

            var genetics = args.EntityManager.EntitySysManager.GetEntitySystem<GenomeSystem>();

            foreach (var mutation in genome.MutatedMutations)
            {
                genetics.CancelMutatorMutation(args.SolutionEntity, genome, mutation);
            }

            foreach (var mutation in genome.ActivatedMutations)
            {
                genetics.CancelActivatorMutation(args.SolutionEntity, genome, mutation);
            }
        }

        // TODO
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            //return Loc.GetString("reagent-effect-guidebook-adjust-reagent-reagent",("chance", Probability), ("deltasign", MathF.Sign(Amount.Float())), ("reagent", reagentProto.LocalizedName), ("amount", MathF.Abs(Amount.Float())));
            //throw new NotImplementedException();
            return "Hello";
        }
    }
}

