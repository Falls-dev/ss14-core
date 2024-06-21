using Content.Server.Power.Components;

namespace Content.Server._White.Lighting.PointLight.RealBattery;

public sealed class PointLightRealBatterySystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightRealBatteryComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    public void ToggleLight(EntityUid uid, string hex, bool enable = true)
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
    }

    public void OnChargeChanged(EntityUid uid, PointLightRealBatteryComponent component, ChargeChangedEvent args)
    {
        var percent = MathF.Round(args.Charge / args.MaxCharge * 100);

        switch (percent)
        {
            case >= 70f:
                ToggleLight(uid, component.GreenColor);
                break;

            case >= 30f and < 70f:
                ToggleLight(uid, component.YellowColor);
                break;

            case < 30f:
                ToggleLight(uid, string.Empty, false);
                break;
        }

    }

}
