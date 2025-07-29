using System.Collections;
using Content.Server._White.Genetics.Components;

namespace Content.Server._White.Genetics;

public sealed class GenomeChangedEvent : EntityEventArgs
{
    public EntityUid Uid;
    public GenomeComponent Comp = default!;
    public Dictionary<string, (BitArray was, BitArray became)> RegionsChanged = default!;

    public GenomeChangedEvent(EntityUid uid, GenomeComponent comp, Dictionary<string, (BitArray was, BitArray became)> regions)
    {
        Uid = uid;
        Comp = comp;
        RegionsChanged = regions;
    }
}
