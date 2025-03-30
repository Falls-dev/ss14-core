using System.Linq;
using Content.Server._White.BodyArmor.ArmorPlates;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Shared._White.BodyArmor;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
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
        SubscribeLocalEvent<PlateCarrierComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlateCarrierComponent, GetPlateDoAfterEvent>(OnGetPlateDoAfter);
        SubscribeLocalEvent<PlateCarrierComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<PlateCarrierComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<PlateCarrierOnUserComponent, DamageModifyEvent>(OnUserGetDamage);
    }

    private void OnExamined(EntityUid uid, PlateCarrierComponent component, ExaminedEvent args)
    {
        var hasPlate = component.HasPlate ? "установлена." : "не установлена.";
        var hasDamage = component.PlateCarrierDamage > 0 ? "имеются визуальные повреждения." : "визуальные повреждения отсутствуют.";

        using (args.PushGroup(nameof(PlateCarrierComponent)))
        {
            args.PushMarkup(Loc.GetString("armorplate-place", ("hasplate", hasPlate)));
            args.PushMarkup(Loc.GetString("platecarrier-damage", ("hasdamage", hasDamage)));
        }
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
                SetPlateCarrierClosed(args.Target, plateCarrierComponent);
            },
            Disabled = false,
            Priority = 3,
            Text = Loc.GetString("platecarrierclosed", ("closed", (plateCarrierComponent.PlateIsClosed ? "Расстегнуть" : "Застегнуть"))),
        };

        args.Verbs.Add(verb);
    }

    private void OnEquipped(EntityUid uid, PlateCarrierComponent component, GotEquippedEvent args)
    {
        if(HasComp<PlateCarrierOnUserComponent>(args.Equipee))
            return;

        var userComp = EnsureComp<PlateCarrierOnUserComponent>(args.Equipee);
        userComp.PlateCarrier = args.Equipment;
    }

    private void OnUnequipped(EntityUid uid, PlateCarrierComponent component, GotUnequippedEvent args)
    {
        UnequipHelper(args.Equipee);
    }

    private void OnUserGetDamage(EntityUid uid, PlateCarrierOnUserComponent component, DamageModifyEvent args)
    {
        if(args.Origin == null)
            return;

        var attacker = args.Origin;

        if (!_handsSystem.TryGetActiveHand((Entity<HandsComponent?>) attacker, out var activeHand))
            return;

        if(activeHand.Container == null)
            return;

        if(!HasComp<GunComponent>(activeHand.Container.ContainedEntities[0]))
            return;

        if(!TryComp<PlateCarrierComponent>(component.PlateCarrier, out var plateCarrierComponent))
            return;

        var intDamage = (int)args.OriginalDamage.DamageDict.First().Value;

        if (!plateCarrierComponent.HasPlate)
        {
            plateCarrierComponent.PlateCarrierDamage += intDamage;
            return;
        }

        plateCarrierComponent.PlateCarrierDamage += (intDamage / 2);
        var armorPlate = GetArmorPlateInContainer((EntityUid)component.PlateCarrier, plateCarrierComponent);

        if(!TryComp<ArmorPlateComponent>(armorPlate, out var armorPlateComponent))
            return;

        armorPlateComponent.ReceivedDamage += (intDamage / 2);

        var newDamageSpecifier = new DamageSpecifier();

        foreach (var damage in args.OriginalDamage.DamageDict)
        {
            newDamageSpecifier.DamageDict.Add(damage.Key, (damage.Value - ApplyDamage(armorPlateComponent)));
        }

        args.Damage = newDamageSpecifier;
    }

    private void SetPlateCarrierClosed(EntityUid platecarrier, PlateCarrierComponent plateCarrierComponent)
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

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
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

    private EntityUid? GetArmorPlateInContainer(EntityUid platecarrier, PlateCarrierComponent component)
    {
        if(!component.HasPlate)
            return null;

        var container =
            _containerSystem.EnsureContainer<Container>(platecarrier, PlateCarrierComponent.ArmorPlateContainer);

        return container.ContainedEntities[0];
    }

    private FixedPoint2 ApplyDamage(ArmorPlateComponent armorPlateComponent)
    {
        if (armorPlateComponent.ReceivedDamage >= armorPlateComponent.AllowedDamage)
            return 0;

        if (armorPlateComponent.ReceivedDamage >= (armorPlateComponent.AllowedDamage / 2))
            return (armorPlateComponent.DamageOfTier[armorPlateComponent.PlateTier] / 2);

        return armorPlateComponent.DamageOfTier[armorPlateComponent.PlateTier];
    }

    private void UnequipHelper(EntityUid user)
    {
        if(!HasComp<PlateCarrierOnUserComponent>(user))
            return;

        RemComp<PlateCarrierOnUserComponent>(user);
    }
}
