using Content.Server.NPC.HTN;
using Content.Shared._White.Weapons.HisGrace;

namespace Content.Server._White.Weapons.HisGrace;

public sealed class HisGraceSystem : SharedHisGraceSystem
{
    protected override void BecomeNpc(EntityUid target)
    {
        var htnComponent = EnsureComp<HTNComponent>(target);
        htnComponent.RootTask = new HTNCompoundTask
        {
            Task = "XenoCompound"
        };
    }

    protected override void RemoveNpc(EntityUid target)
    {
        RemComp<HTNComponent>(target);
    }
}