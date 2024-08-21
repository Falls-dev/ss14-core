using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.GameTicking;
using Content.Shared.Item;
using Robust.Shared.Random;

namespace Content.Server._White.RandomArtifacts;

public sealed class RandomArtifactsSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float ItemToArtifactRatio = 0.7f; // from 0 to 100

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);
    }

    private void OnRoundStart(RoundStartedEvent ev)
    {
        var items = EntityQuery<ItemComponent>().ToList();
        _random.Shuffle(items);

        var selectedItems = GetPercentageOfHashSet(items, ItemToArtifactRatio);

        foreach (var item in selectedItems)
        {
            var entity = item.Owner;

            var artifactComponent = EnsureComp<ArtifactComponent>(entity);
            _artifactsSystem.RandomizeArtifact(entity, artifactComponent);
        }
    }

    private HashSet<ItemComponent> GetPercentageOfHashSet(List<ItemComponent> sourceList, float percentage)
    {
        var countToAdd = (int) Math.Round((double) sourceList.Count * percentage / 100);

        return sourceList.Where(x => !Transform(x.Owner).Anchored).Take(countToAdd).ToHashSet();
    }
}

/*
    Number of items on maps
    DEV - 1527
    WhiteBox - 13692
    WonderBox - 15306
*/
