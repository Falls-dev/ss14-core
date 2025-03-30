using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Shared._White.BodyArmor;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server._White.BodyArmor.PlateCarrier;

public sealed class PlateCarrierSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<PlateCarrierComponent, GetPlateDoAfterEvent>(OnGetPlateDoAfter);
    }

    private void OnAltVerb(GetVerbsEvent<AlternativeVerb> args)
    {
        var platecarrier = args.Target;
        if(!TryComp<PlateCarrierComponent>(platecarrier, out var plateCarrierComponent))
            return;

        if (plateCarrierComponent is { HasPlate: true, PlateIsClosed: false })
        {
            AlternativeVerb plateVerb = new()
            {
                Act = () =>
                {
                    GetArmorPlate(args.User, args.Target, plateCarrierComponent);
                },
                Disabled = false,
                Priority = 1,
                Text = Loc.GetString("getplate"),
            };
            args.Verbs.Add(plateVerb);
        }

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                SetPlateCarrierClosed(args.User, args.Target, plateCarrierComponent);
            },
            Disabled = false,
            Priority = 3,
            Text = Loc.GetString("platecarrierclosed", ("closed", (plateCarrierComponent.PlateIsClosed ? "Расстегнуть" : "Застегнуть"))),
        };

        args.Verbs.Add(verb);
    }

    private void SetPlateCarrierClosed(EntityUid uid, EntityUid platecarrier, PlateCarrierComponent plateCarrierComponent)
    {
        _audioSystem.PlayPvs((plateCarrierComponent.PlateIsClosed ? plateCarrierComponent.OpenSound : plateCarrierComponent.CloseSound), platecarrier.ToCoordinates());
        plateCarrierComponent.PlateIsClosed = !plateCarrierComponent.PlateIsClosed;
    }

    private void GetArmorPlate(EntityUid uid, EntityUid platecarrier, PlateCarrierComponent plateCarrierComponent)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            uid,
            plateCarrierComponent.TimeToPutPlate,
            new GetPlateDoAfterEvent(),
            platecarrier,
            target: platecarrier)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        if(!_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            return;
    }

    private void OnGetPlateDoAfter(EntityUid uid, PlateCarrierComponent component, GetPlateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if(!component.HasPlate)
            return;

        if(args.Target == null)
            return;

        var platecarrier = (EntityUid)args.Target;

        var armorPlateContainer =
            _containerSystem.EnsureContainer<Container>(platecarrier, PlateCarrierComponent.ArmorPlateContainer);

        var plates = armorPlateContainer.ContainedEntities.ToList();

        _containerSystem.EmptyContainer(armorPlateContainer, true);
        foreach (var plate in plates)
        {
            _handsSystem.PickupOrDrop(args.User, plate);
        }
        component.HasPlate = false;

        args.Handled = true;
    }
}
