using Content.Shared.Movement.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server._White.Genetics.Mutations
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class GlowingMutation : MutationEffect
    {
        // Issues will arise if mutation is added to already glowing entity.
        public override void Apply(EntityUid uid, IEntityManager entityManager)
        {
            var light = new PointLightComponent();
            entityManager.AddComponent(uid, light);
        }

        public override void Cancel(EntityUid uid, IEntityManager entityManager)
        {
            entityManager.RemoveComponent<PointLightComponent>(uid);
        }
    }
}
