using Content.Server.DeviceLinking.Events;
using Content.Shared._White.MechComp;
using System.Linq;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{

    private void InitComparer()
    {
        SubscribeLocalEvent<MechCompComparerComponent, ComponentInit>(OnComparerInit);
        SubscribeLocalEvent<MechCompComparerComponent, MechCompConfigAttemptEvent>(OnComparerConfigAttempt);
        SubscribeLocalEvent<MechCompComparerComponent, MechCompConfigUpdateEvent>(OnComparerConfigUpdate);
        SubscribeLocalEvent<MechCompComparerComponent, SignalReceivedEvent>(OnComparerSignal);
    }

    private Dictionary<string, Func<string, string, bool?>> _compareFuncs = new()
    {
        ["A==B"] = (a, b) => { return a == b; },
        ["A!=B"] = (a, b) => { return a != b; },
        ["A>B"] = (a, b) => { if (float.TryParse(a, out var numA) && float.TryParse(b, out var numB)) return numA > numB; else return null; },
        ["A<B"] = (a, b) => { if (float.TryParse(a, out var numA) && float.TryParse(b, out var numB)) return numA < numB; else return null; },
        ["A>=B"] = (a, b) => { if (float.TryParse(a, out var numA) && float.TryParse(b, out var numB)) return numA >= numB; else return null; },
        ["A<=B"] = (a, b) => { if (float.TryParse(a, out var numA) && float.TryParse(b, out var numB)) return numA <= numB; else return null; },
    };
    public void OnComparerInit(EntityUid uid, MechCompComparerComponent comp, ComponentInit args)
    {
        //EnsureConfig(uid).Build(
        //("valueA", (typeof(string), "Значение A", "0")),
        //("valueB", (typeof(string), "Значение B", "0")),
        //("outputTrue", (typeof(string), "Значение на выходе в случае истины", "1")),
        //("outputFalse", (typeof(string), "Значение на выходе в случае лжи", "1")),
        //
        //("mode", (typeof(string), "Режим", _compareFuncs.Keys.First(), _compareFuncs.Keys)),
        //("_", (null, "Режимы сравнения >, <, >=, <=")), // todo: check if newlines work
        //("__", (null, "работают только с числовыми значениями."))
        //);
        if (!_compareFuncs.ContainsKey(comp.mode))
            comp.mode = _compareFuncs.Keys.First();
        _link.EnsureSinkPorts(uid, "MechCompInputA", "MechCompInputB");
        _link.EnsureSourcePorts(uid, "MechCompLogicOutputTrue", "MechCompLogicOutputFalse");


    }

    private void OnComparerConfigAttempt(EntityUid uid, MechCompComparerComponent comp, MechCompConfigAttemptEvent args)
    {
        args.entries.Add((typeof(float), "Число A", comp.A));
        args.entries.Add((typeof(float), "Число B", comp.B));
        args.entries.Add((typeof(string), "Значение на выходе в случае истины", comp.outputTrue));
        args.entries.Add((typeof(string), "Значение на выходе в случае лжи", comp.outputFalse));
        args.entries.Add((typeof(List<string>), "Операция", comp.mode, _compareFuncs.Keys));
        args.entries.Add((null, "Режимы сравнения >, <, >=, <=")); // todo: check if newlines work
        args.entries.Add((null, "работают только с числовыми значениями."));
    }

    private void OnComparerConfigUpdate(EntityUid uid, MechCompComparerComponent comp, MechCompConfigUpdateEvent args)
    {

        comp.A = (string) args.results[0];
        comp.B = (string) args.results[1];
        comp.outputTrue = (string) args.results[2];
        comp.outputFalse = (string) args.results[3];
        comp.mode = (string) args.results[4];
    }

    public void OnComparerSignal(EntityUid uid, MechCompComparerComponent comp, ref SignalReceivedEvent args)
    {
        string sig;
        switch (args.Port)
        {
            case "MechCompNumericInputA":
                if (TryGetMechCompSignal(args.Data, out sig))
                {
                    comp.A = sig;
                }
                break;
            case "MechCompNumericInputB":
                if (TryGetMechCompSignal(args.Data, out sig))
                {
                    comp.B = sig;
                }
                break;
            case "Trigger":
                bool? result = _compareFuncs[comp.mode](comp.A, comp.B);
                switch (result)
                {
                    case true:
                        SendMechCompSignal(uid, "MechCompLogicOutputTrue", comp.outputTrue);
                        break;
                    case false:
                        SendMechCompSignal(uid, "MechCompLogicOutputFalse", comp.outputFalse);
                        break;
                    case null:
                        break;

                }
                break;
        }
    }

}
