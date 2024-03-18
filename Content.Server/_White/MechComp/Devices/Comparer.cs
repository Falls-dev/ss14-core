using Content.Server.DeviceLinking.Events;
using Content.Shared._White.MechComp;
using System.Linq;


namespace Content.Server._White.MechComp;

public sealed partial class MechCompDeviceSystem
{

    private void InitComparer()
    {
        SubscribeLocalEvent<MechCompComparerComponent, ComponentInit>(OnComparerInit);
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
        EnsureConfig(uid).Build(
        ("valueA", (typeof(string), "Значение A", "0")),
        ("valueB", (typeof(string), "Значение B", "0")),
        ("outputTrue", (typeof(string), "Значение на выходе в случае истины", "1")),
        ("outputFalse", (typeof(string), "Значение на выходи в случае лжи", "1")),

        ("mode", (typeof(string), "Режим", _compareFuncs.Keys.First(), _compareFuncs.Keys)),
        ("_", (null, "Режимы сравнения >, <, >=, <=")), // todo: check if newlines work
        ("__", (null, "работают только с числовыми значениями."))
        );
        _link.EnsureSinkPorts(uid, "MechCompInputA", "MechCompInputB");
        _link.EnsureSourcePorts(uid, "MechCompLogicOutputA", "MechCompLogicOutputB");

    }

    public void OnComparerSignal(EntityUid uid, MechCompComparerComponent comp, ref SignalReceivedEvent args)
    {
        string sig;
        var cfg = GetConfig(uid);
        switch (args.Port)
        {
            case "MechCompNumericInputA":
                if (TryGetMechCompSignal(args.Data, out sig))
                {
                    SetConfigString(uid, "valueA", sig);
                }
                break;
            case "MechCompNumericInputB":
                if (TryGetMechCompSignal(args.Data, out sig))
                {
                    SetConfigString(uid, "valueB", sig);
                }
                break;
            case "Trigger":
                string valA = GetConfigString(uid, "ValueA");
                string valB = GetConfigString(uid, "ValueB");
                bool? result = _compareFuncs[GetConfigString(uid, "mode")](valA, valB);
                switch (result)
                {
                    case true:
                        SendMechCompSignal(uid, "MechCompLogicOutputTrue", GetConfigString(uid, "outputTrue"));
                        break;
                    case false:
                        SendMechCompSignal(uid, "MechCompLogicOutputFalse", GetConfigString(uid, "outputFalse"));
                        break;
                    case null:
                        break;

                }
                break;
        }
    }

}
