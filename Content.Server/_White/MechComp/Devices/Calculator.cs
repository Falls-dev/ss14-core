using Content.Server.DeviceLinking.Events;
using Content.Shared._White.MechComp;
using Robust.Shared.Utility;
using System.Linq;



namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{
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

    private void InitCalculator()
    {
        SubscribeLocalEvent<MechCompMathComponent, ComponentInit>(OnMathInit);
        SubscribeLocalEvent<MechCompMathComponent, MechCompConfigAttemptEvent>(OnMathConfigAttempt);
        SubscribeLocalEvent<MechCompMathComponent, MechCompConfigUpdateEvent>(OnMathConfigUpdate);
        SubscribeLocalEvent<MechCompMathComponent, SignalReceivedEvent>(OnMathSignal);
    }
    public void OnMathInit(EntityUid uid, MechCompMathComponent comp, ComponentInit args)
    {
        if (!_mathFuncs.ContainsKey(comp.mode))
            comp.mode = _mathFuncs.Keys.First();

        _link.EnsureSinkPorts(uid, "MechCompNumericInputA", "MechCompNumericInputB", "Trigger");
        _link.EnsureSourcePorts(uid, "MechCompNumericOutput");
    }

    private void OnMathConfigAttempt(EntityUid uid, MechCompMathComponent comp, MechCompConfigAttemptEvent args)
    {
        args.entries.Add((typeof(List<string>), "Операция", comp.mode, _mathFuncs.Keys));
        args.entries.Add((typeof(float), "Число A", comp.A));
        args.entries.Add((typeof(float), "Число B", comp.B));
    }

    private void OnMathConfigUpdate(EntityUid uid, MechCompMathComponent comp, MechCompConfigUpdateEvent args)
    {
        comp.mode = (string) args.results[0];
        comp.A = (float) args.results[1];
        comp.B = (float) args.results[2];
    }


    public void OnMathSignal(EntityUid uid, MechCompMathComponent comp, ref SignalReceivedEvent args)
    {
        string sig; float num; // hurr durr
        switch (args.Port)
        {
            case "MechCompNumericInputA":
                if(TryGetMechCompSignal(args.Data, out sig) && float.TryParse(sig, out num))
                {
                    comp.A = num;
                }
                break;
            case "MechCompNumericInputB":
                if (TryGetMechCompSignal(args.Data, out sig) && float.TryParse(sig, out num))
                {
                    comp.B = num;
                }
                break;
            case "Trigger":
                float? result = _mathFuncs[comp.mode](comp.A, comp.B);
                if (result != null)
                {
                    SendMechCompSignal(uid, "MechCompNumericOutput", result.ToString()!);
                }
                break;
        }
    }


}
