using JetBrains.Annotations;

namespace Content.Server._White.Genetics
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract partial class MutationEffect
    {
        public abstract void Apply(EntityUid appliedEntity, IEntityManager entityManager);
        public abstract void Cancel(EntityUid appliedEntity, IEntityManager entityManager);
    }
}
