using Content.Server._White.BodyArmor.PlateCarrier;
using Content.Server.DoAfter;
using Content.Shared._White.BodyArmor;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server._White.BodyArmor.ArmorPlates;

public sealed class ArmorPlateSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorPlateComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ArmorPlateComponent, PutPlateDoAfterEvent>(OnPutPlateDoAfter);
    }

    private void OnInteract(EntityUid uid, ArmorPlateComponent component, AfterInteractEvent args)
    {
        var platecarrier = args.Target;

        if(!TryComp<PlateCarrierComponent>(platecarrier, out var plateCarrierComponent))
            return;

        if(plateCarrierComponent.PlateIsClosed)
            return;

        if(plateCarrierComponent.HasPlate)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            plateCarrierComponent.TimeToPutPlate,
            new PutPlateDoAfterEvent(),
            uid,
            used: uid,
            target: platecarrier)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        if(!_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            return;

        args.Handled = true;
    }

    private void OnPutPlateDoAfter(EntityUid uid, ArmorPlateComponent component, PutPlateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if(args.Target == null || args.Used == null)
            return;

        var platecarrier = (EntityUid)args.Target;

        if (!TryComp<PlateCarrierComponent>(platecarrier, out var plateCarrierComponent))
            return;

        var armorplate = (EntityUid)args.Used;

        var armorPlateContainer =
            _containerSystem.EnsureContainer<Container>(platecarrier, PlateCarrierComponent.ArmorPlateContainer);

        _containerSystem.Insert(armorplate, armorPlateContainer);
        plateCarrierComponent.HasPlate = true;

        args.Handled = true;
    }
}
