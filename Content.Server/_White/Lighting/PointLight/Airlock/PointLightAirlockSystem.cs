using Content.Shared._White.Lighting;
using Content.Shared.Doors.Components;

namespace Content.Server._White.Lighting.PointLight.Airlock;

public sealed class PointLightAirlockSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightAirlockComponent, DoorlightsChangedEvent>(OnDoorLightChanged);
    }

    private void ToggleLight(EntityUid uid, string hex, bool value)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        var color = Color.FromHex(hex);

        _pointLightSystem.SetColor(uid, color, pointLightComponent);
        _pointLightSystem.SetEnabled(uid, value, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(value), true);
    }

    private void OnDoorLightChanged(EntityUid uid, PointLightAirlockComponent component, DoorlightsChangedEvent args)
    {
        if (!TryComp<DoorComponent>(uid, out var doorComponent))
            return;


        switch (args.State)
        {
            case DoorVisuals.BoltLights:
                ToggleLight(uid, component.BoltedColor, args.Value);
                break;

            case DoorVisualLayers.BaseUnlit:
                ToggleLight(uid, component.PoweredColor, args.Value);
                break;

            case DoorVisualLayers.BaseEmergencyAccess:
                ToggleLight(uid, component.EmergencyLightsColor, args.Value);
                break;

            case DoorVisuals.ClosedLights:
                ToggleLight(uid, component.BoltedColor, args.Value);
                break;

            default:
                ToggleLight(uid, component.BoltedColor, false);
                break;
        }

    }
}
