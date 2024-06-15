using Content.Shared.Movement.Systems;
using JetBrains.Annotations;

namespace Content.Shared._White.Genetics.Mutations
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class XrayMutation : MutationEffect
    {
        public override void Effect(MutationEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent<EyeComponent>(args.AppliedEntity, out var eye))
            {
                var eyes = args.EntityManager.SystemOrNull<SharedContentEyeSystem>();
            }
        }
    }
}
