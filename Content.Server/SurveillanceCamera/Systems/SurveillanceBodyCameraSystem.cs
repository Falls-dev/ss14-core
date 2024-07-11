using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.PowerCell.Components;
using Content.Shared.Toggleable;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;

namespace Content.Server.SurveillanceCamera.Systems;

/// <summary>
/// This handles the bodycamera all itself. Activation, examine,init, powercell stuff.
/// </summary>
public sealed class SurveillanceBodyCameraSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceBodyCameraComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ComponentInit>(OnInit);
    }

    public void OnInit(EntityUid uid, SurveillanceBodyCameraComponent comp, ComponentInit args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        _surveillanceCameras.SetActive(uid, false, surComp);
        surComp.NetworkSet = true;
        AppearanceChange(uid, surComp.Active);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SurveillanceBodyCameraComponent>();
        while (query.MoveNext(out var uid, out var cam))
        {
            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
                continue;

            if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
                continue;

            if (!surComp.Active)
                continue;

            // WD EDIT START
            if (_battery.TryUseCharge(uid, cam.Wattage * frameTime, battery))
                continue;

            var message = Loc.GetString("surveillance-body-camera-off",
                ("item", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(message, uid, Filter.PvsExcept(uid, entityManager: EntityManager), true);
            _surveillanceCameras.SetActive(uid, false, surComp);
            AppearanceChange(uid, surComp.Active);
            // WD EDIT END
        }
    }

    private void OnPowerCellChanged(EntityUid uid, SurveillanceBodyCameraComponent comp, PowerCellChangedEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        // WD EDIT START
        if (!args.Ejected)
            return;

        if (surComp.Active)
        {
            var message = Loc.GetString("surveillance-body-camera-off",
                ("item", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(message, uid, Filter.PvsExcept(uid, entityManager: EntityManager), true);
        }

        _surveillanceCameras.SetActive(uid, false, surComp);
        AppearanceChange(uid, surComp.Active);
        // WD EDIT END
    }

    public void OnExamine(EntityUid uid, SurveillanceBodyCameraComponent comp, ExaminedEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (args.IsInDetailsRange)
        {
            var message =
                Loc.GetString(surComp.Active ? "surveillance-body-camera-on" : "surveillance-body-camera-off",
                    ("item", Identity.Entity(uid, EntityManager))); // WD EDIT
            args.PushMarkup(message);
        }
    }

    public void AppearanceChange(EntityUid uid, Boolean isActive)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance) &&
            TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, isActive ? "on" : "off", false, item);
            _clothing.SetEquippedPrefix(uid, isActive ? null : "off");
            _appearance.SetData(uid, ToggleVisuals.Toggled, isActive, appearance);
        }
    }
}
