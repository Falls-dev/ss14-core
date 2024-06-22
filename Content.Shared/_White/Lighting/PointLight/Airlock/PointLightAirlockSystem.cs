using Content.Shared.Doors.Components;

namespace Content.Shared._White.Lighting.PointLight.Airlock;

//TODO: Когда-нибудь починить эту хуйню: Когда дверь открыта на аварийный доступ и ее болтируют, то свет будет желтым, хотя должен быть красным из-за болтов.

public sealed class SharedPointLightAirlockSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightAirlockComponent, DoorlightsChangedEvent>(OnDoorLightChanged);
    }

    public void ToggleLight(EntityUid uid, string hex, PointLightAirlockComponent airlockLight, bool enable = true)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        if (enable)
        {
            var color = Color.FromHex(hex);
            _pointLightSystem.SetColor(uid, color, pointLightComponent);
        }

        _pointLightSystem.SetEnabled(uid, enable, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(enable), true);
        airlockLight.IsLightsEnabled = enable;
    }

    public void OnDoorLightChanged(EntityUid uid, PointLightAirlockComponent component, DoorlightsChangedEvent args)
    {
        if (!TryComp<DoorComponent>(uid, out var door))
            return;

        if (!component.IsPowered)
        {
            if (component.IsLightsEnabled)
                ToggleLight(uid, string.Empty, component, false);

            return;
        }


        if (TryComp<AirlockComponent>(uid, out var airlockComponent) && airlockComponent.EmergencyAccess && args.Value && args.State is not DoorVisuals.EmergencyLights && args.State != null)
            return; // While emergency access lights must be yellow no matter what

        switch (args.State)
        {
            case DoorVisuals.BoltLights:
                if (args.Value)
                    ToggleLight(uid, component.RedColor, component);
                else
                    RaiseLocalEvent(uid, new DoorlightsChangedEvent(door.State, true));
                break;

            case DoorState.Denying:
                ToggleLight(uid, component.RedColor, component);
                break;

            case DoorState.Closed:
                ToggleLight(uid, component.BlueColor, component);
                break;

            case DoorVisuals.EmergencyLights:
                if (args.Value)
                    ToggleLight(uid, component.YellowColor, component);
                else
                    RaiseLocalEvent(uid, new DoorlightsChangedEvent(door.State, true));
                break;

            case DoorState.Open:
                ToggleLight(uid, component.BlueColor, component);
                break;

            case DoorState.Opening:
                ToggleLight(uid, component.GreenColor, component);
                break;

            case DoorState.Closing:
                ToggleLight(uid, component.GreenColor, component);
                break;

            default:
                ToggleLight(uid, string.Empty, component, false);
                break;
        }

    }

}
