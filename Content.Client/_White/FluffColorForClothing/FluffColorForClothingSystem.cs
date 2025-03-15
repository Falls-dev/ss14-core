using Content.Shared._White.FluffColorForClothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client._White.FluffColorForClothing;

public sealed class FluffColorForClothingSystem : SharedFluffColorForClothingSystem
{
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;

    protected override void UpdateVisuals(Entity<FluffColorForClothingComponent> entity)
    {
        if (!TryComp(entity, out SpriteComponent? sprite))
            return;

        var state = sprite.LayerGetState(0).Name;

        if (state == null)
            return;

        var prefix = state.Substring(0, state.IndexOf('_'));
        sprite.LayerSetState(0, $"{prefix}_{entity.Comp.CurrentColor}");

        if (TryComp<ClothingComponent>(entity, out var clothingComp))
            _clothingSystem.SetEquippedPrefix(entity, entity.Comp.CurrentColor, clothingComp);

        if (TryComp<ItemComponent>(entity, out var itemComp))
            _itemSystem.SetHeldPrefix(entity, entity.Comp.CurrentColor, false, itemComp);
    }

}
