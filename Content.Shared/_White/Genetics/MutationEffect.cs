using JetBrains.Annotations;

namespace Content.Shared._White.Genetics
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract partial class MutationEffect
    {
        public abstract void Effect(MutationEffectArgs args);
    }

    public readonly record struct MutationEffectArgs(EntityUid AppliedEntity, IEntityManager EntityManager);
}
