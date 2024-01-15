using Content.Shared.Inventory.Events;
using Content.Shared.White.MagGloves;

namespace Content.Server.White.MagGloves;

/// <summary>
/// This handles...
/// </summary>
public sealed class MagneticGlovesSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MagneticGlovesComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagneticGlovesComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    public void OnGotUnequipped(EntityUid uid, MagneticGlovesComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "gloves")
        {
            ToggleGloves(args.Equipee, component, false);
        }
    }

    public void OnGotEquipped(EntityUid uid, MagneticGlovesComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "gloves")
        {
            ToggleGloves(args.Equipee, component, true);
        }
    }

    public void ToggleGloves(EntityUid owner, MagneticGlovesComponent component, bool active)
    {
        if (!active)
        {
            RemComp<KeepItemsOnFallComponent>(owner);
        }
        else if (component.Enabled)
        {
            EnsureComp<KeepItemsOnFallComponent>(owner);
        }
    }
}
