using Content.Shared.Doors.Components;

namespace Content.Shared._White.Lighting.PointLight.Airlock;

public sealed class PointLightAirlockSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightAirlockComponent, DoorlightsChangedEvent>(OnDoorLightChanged);
    }

    private void ToggleLight(EntityUid uid, string? hex, bool value)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        if (value)
        {
            var color = Color.FromHex(hex);
            _pointLightSystem.SetColor(uid, color, pointLightComponent);
        }
        else if (TryComp<DoorComponent>(uid, out var doorComponent) && hex != null)
        {
            RaiseLocalEvent(uid, new DoorlightsChangedEvent(doorComponent.State, true));
        }

        _pointLightSystem.SetEnabled(uid, value, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(value), true);
    }

    private void OnDoorLightChanged(EntityUid uid, PointLightAirlockComponent component, DoorlightsChangedEvent args)
    {
        if (!HasComp<DoorComponent>(uid))
            return;

        switch (args.State)
        {
            case DoorVisuals.BoltLights:
                ToggleLight(uid, component.RedColor, args.Value);
                break;

            case DoorState.Denying:
                ToggleLight(uid, component.RedColor, args.Value);
                break;

            case DoorState.Closed:
                ToggleLight(uid, component.BlueColor, args.Value);
                break;

            case DoorVisuals.EmergencyLights:
                ToggleLight(uid, component.YellowColor, args.Value);
                break;

            case DoorState.Open:
                ToggleLight(uid, component.BlueColor, args.Value);
                break;

            case DoorState.Opening:
                ToggleLight(uid, component.GreenColor, args.Value);
                break;

            case DoorState.Closing:
                ToggleLight(uid, component.GreenColor, args.Value);
                break;

            default:
                ToggleLight(uid, null, false);
                break;
        }

    }
}
