using Content.Shared.Atmos;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Examine; // WD
using Content.Shared.Verbs; // WD
using Robust.Shared.Utility; // WD

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Handles reducing fire damage when wearing clothing with <see cref="FireProtectionComponent"/>.
/// </summary>
public sealed class FireProtectionSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!; // WD

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireProtectionComponent, InventoryRelayedEvent<GetFireProtectionEvent>>(OnGetProtection);
        SubscribeLocalEvent<FireProtectionComponent, GetVerbsEvent<ExamineVerb>>(OnProtectionVerbExamine); // WD
    }

    private void OnGetProtection(Entity<FireProtectionComponent> ent, ref InventoryRelayedEvent<GetFireProtectionEvent> args)
    {
        args.Args.Reduce(ent.Comp.Reduction);
    }

    // WD EDIT START
    private void OnProtectionVerbExamine(EntityUid uid, FireProtectionComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var modifierPercentage = MathF.Round(component.Reduction * 100f, 1);

        if (modifierPercentage == 0.0f)
            return;

        var msg = new FormattedMessage();

        msg.AddMarkup(Loc.GetString("fire-protection-examine", ("modifier", modifierPercentage)));

        _examine.AddDetailedExamineVerb(args, component, msg,
            Loc.GetString("fire-protection-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("fire-protection-examinable-verb-message"));
    }
    // WD EDIT END
}
