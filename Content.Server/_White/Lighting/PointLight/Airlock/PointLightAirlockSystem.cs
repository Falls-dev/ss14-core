using Content.Shared._White.Lighting;
using Content.Shared.Doors.Components;

namespace Content.Server._White.Lighting.PointLight.Airlock;

public sealed class PointLightAirlockSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightAirlockComponent, AppearanceChangedEvent>(OnDoorLightChanged);
    }

    private void EnableLight(EntityUid uid, string hex)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        var color = Color.FromHex(hex);

        _pointLightSystem.SetColor(uid, color, pointLightComponent);
        _pointLightSystem.SetEnabled(uid, true, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(true), true);
    }

    private void DisableLight(EntityUid uid)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        _pointLightSystem.SetEnabled(uid, false, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(false), true);
    }

    private void OnDoorLightChanged(EntityUid uid, PointLightAirlockComponent component, AppearanceChangedEvent args)
    {
        if (!TryComp<DoorComponent>(uid, out var doorComponent))
            return;


        switch (args.State)
        {
            case DoorVisualLayers.BaseBolted:
                EnableLight(uid, component.BoltedColor);
                break;

            case DoorVisualLayers.BaseUnlit:
                EnableLight(uid, component.PoweredColor);
                break;

            case DoorVisualLayers.BaseEmergencyAccess:
                EnableLight(uid, component.EmergencyLightsColor);
                break;

            case DoorVisuals.ClosedLights:
                EnableLight(uid, component.BoltedColor);
                break;

            default:
                DisableLight(uid);
                break;
        }

    }
}
