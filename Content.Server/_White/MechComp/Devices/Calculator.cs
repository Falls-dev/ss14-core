using Content.Server.DeviceLinking.Events;
using Content.Shared._White.MechComp;
using Robust.Shared.Utility;
using System.Linq;



namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
    private void InitCalculator()
    {
        SubscribeLocalEvent<MechCompMathComponent, ComponentInit>(OnMathInit);
        SubscribeLocalEvent<MechCompMathComponent, SignalReceivedEvent>(OnMathSignal);
    }



    private Dictionary<string, Func<float, float, float?>> _mathFuncs = new()
    {
        ["A+B"] = (a, b) => { return a + b; },
        ["A-B"] = (a, b) => { return a - b; },
        ["A*B"] = (a, b) => { return a * b; },
        ["A/B"] = (a, b) => { if (b == 0) return null; return a / b; },
        ["A^B"] = (a, b) => { return MathF.Pow(a, b); },
        ["A//B"] = (a, b) => { return (float) (int) (a / b); },
        ["A%B"] = (a, b) => { return a % b; },
        ["sin(A)^B"] = (a, b) => { return MathF.Pow(MathF.Sin(a), b); },
        ["cos(A)^B"] = (a, b) => { return MathF.Pow(MathF.Cos(a), b); }
    };
    public void OnMathInit(EntityUid uid, MechCompMathComponent comp, ComponentInit args)
    {
        EnsureConfig(uid).Build(
            ("mode", (typeof(List<string>), "Операция", _mathFuncs.Keys.First(), _mathFuncs.Keys.ToArray()) ),
            ("numberA", (typeof(float), "Число A", "0") ),
            ("numberB", (typeof(float), "Число B", "0") )
        );
        _link.EnsureSinkPorts(uid, "MechCompNumericInputA", "MechCompNumericInputB", "Trigger");
        _link.EnsureSourcePorts(uid, "MechCompNumericOutput");
    }

    public void OnMathSignal(EntityUid uid, MechCompMathComponent comp, ref SignalReceivedEvent args)
    {
        string sig; float num; // hurr durr
        var cfg = GetConfig(uid);
        switch (args.Port)
        {
            case "MechCompNumericInputA":
                if(TryGetMechCompSignal(args.Data, out sig) && float.TryParse(sig, out num))
                {
                    SetConfigFloat(uid, "numberA", num);
                }
                break;
            case "MechCompNumericInputB":
                if (TryGetMechCompSignal(args.Data, out sig) && float.TryParse(sig, out num))
                {
                    SetConfigFloat(uid, "numberB", num);
                }
                break;
            case "Trigger":
                float numA = GetConfigFloat(uid, "numberA");
                float numB = GetConfigFloat(uid, "numberB");
                float? result = _mathFuncs[GetConfigString(uid, "mode")](numA, numB);
                if (result != null)
                {
                    SendMechCompSignal(uid, "MechCompNumericOutput", result.ToString()!);
                }
                break;
        }
    }


}
