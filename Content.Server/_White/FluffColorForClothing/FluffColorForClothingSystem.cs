using System.Linq;
using Content.Shared._White.FluffColorForClothing;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Server._White.FluffColorForClothing;

public sealed class FluffColorForClothingSystem : SharedFluffColorForClothingSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private string GetNextColor(FluffColorForClothingComponent component)
    {
        var index = component.Colors.IndexOf(component.CurrentColor);
        var count = component.Colors.Count;
        if (index < count - 1)
            index++;
        else
            index = 0;

        var newColor = component.Colors[index];

        return newColor;
    }

    protected override void ChangeColor(Entity<FluffColorForClothingComponent> entity)
    {
        if (entity.Comp.User != null && _inventory.TryGetContainerSlotEnumerator((EntityUid) entity.Comp.User, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.NextItem(out var item, out var _))
            {
                if (TryComp<FluffColorForClothingComponent>(item, out var comp) && !comp.MainItem)
                {
                    comp.CurrentColor = GetNextColor(comp);
                    Dirty(item, comp);
                }
            }
        }

        ChangeCompInside(entity);
        entity.Comp.CurrentColor = GetNextColor(entity);

        Dirty(entity);
    }

    private void ChangeCompInside(Entity<FluffColorForClothingComponent> entity)
    {
        if (_container.TryGetContainer(entity, "toggleable-clothing", out var container) && container.ContainedEntities.Any())
        {
            var content = container.ContainedEntities.First();
            if (TryComp<FluffColorForClothingComponent>(content, out var contentComp) && entity.Comp.Specifier == contentComp.Specifier)
            {
                contentComp.CurrentColor = GetNextColor(contentComp);
                Dirty(entity, contentComp);
            }

        }
    }
}
