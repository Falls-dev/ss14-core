using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.SurveillanceCamera;
using Content.Server.SurveillanceCamera.Systems;
using Content.Shared.Actions;
using Content.Shared._White.SurveillanceCamera;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;

namespace Content.Server._White.SurveillanceCamera;

public sealed class SurveillanceBodyCameraWhiteSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private readonly SurveillanceBodyCameraSystem _surveillanceBodyCameras = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ToggleBodyCameraEvent>(OnToggleAction);
    }

    private void OnStartup(EntityUid uid, SurveillanceBodyCameraComponent component, ComponentStartup args)
    {
        EnsureComp(uid, out SurveillanceCameraComponent surComp);
        _surveillanceCameras.UpdateSetupInterface(uid, surComp);
    }

    private void OnGetActions(EntityUid uid, SurveillanceBodyCameraComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnToggleAction(EntityUid uid, SurveillanceBodyCameraComponent component, ToggleBodyCameraEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return;

        _surveillanceCameras.SetActive(uid, battery.CurrentCharge > component.Wattage && !surComp.Active, surComp);
        _surveillanceBodyCameras.AppearanceChange(uid, surComp.Active);

        var message = Loc.GetString(surComp.Active ? "surveillance-body-camera-on" : "surveillance-body-camera-off",
            ("item", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(message, uid, Filter.PvsExcept(uid, entityManager: EntityManager), true);
    }
}
