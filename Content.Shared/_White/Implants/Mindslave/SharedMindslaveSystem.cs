using Content.Shared._White.Implants.Mindslave.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Tag;

namespace Content.Shared._White.Implants.Mindslave;

public sealed class SharedMindslaveSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    private const string MindslaveTag = "MindSlave";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, AddImplantAttemptEvent>(OnTryInsertMindslave);
        SubscribeLocalEvent<SubdermalImplantComponent, SubdermalImplantInserted>(OnMindslaveInserted);
    }

    private void OnTryInsertMindslave(Entity<MindContainerComponent> ent, ref AddImplantAttemptEvent args)
    {
        if (!_tag.HasTag(args.Implant, MindslaveTag))
        {
            return;
        }

        if (HasComp<MindShieldComponent>(args.Target) ||
            HasComp<MindslaveComponent>(args.Target) ||
            args.Target == args.User)
        {
            args.Cancel();
        }
    }

    private void OnMindslaveInserted(Entity<SubdermalImplantComponent> ent, ref SubdermalImplantInserted args)
    {
        if (!_tag.HasTag(ent.Owner, MindslaveTag))
        {
            return;
        }

        var slaveComponent = EnsureComp<MindslaveComponent>(args.Target);
        slaveComponent.Slaves.Add(GetNetEntity(args.Target));
        slaveComponent.Master = GetNetEntity(args.User);

        var masterComponent = EnsureComp<MindslaveComponent>(args.User);
        masterComponent.Slaves.Add(GetNetEntity(args.Target));
        masterComponent.Master = GetNetEntity(args.User);
    }
}
