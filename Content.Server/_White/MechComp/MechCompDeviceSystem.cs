using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.VoiceMask;
using Content.Shared._White.MechComp;
using Content.Shared.Chemistry.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Verbs;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;


namespace Content.Server._White.MechComp;

public sealed class MechCompConfigUpdateEvent : EntityEventArgs
{

}

public sealed partial class MechCompDeviceSystem : SharedMechCompDeviceSystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly QuickDialogSystem _dialog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

    Dictionary<(EntityUid, string), (TimeSpan, Action?)> _timeSpans = new();
#region Helper functions
#region mechcomp config get/set functions
    private MechCompConfig GetConfig(EntityUid uid) { return Comp<BaseMechCompComponent>(uid).config;}
    private int GetConfigInt(EntityUid uid, string key) { return GetConfig(uid).GetInt(key); }
    private float GetConfigFloat(EntityUid uid, string key) { return GetConfig(uid).GetFloat(key); }
    private string GetConfigString(EntityUid uid, string key) { return GetConfig(uid).GetString(key); }
    private bool GetConfigBool(EntityUid uid, string key) { return GetConfig(uid).GetBool(key); }
    private void SetConfigInt(EntityUid uid, string key, int value) { GetConfig(uid).SetInt(key, value); }
    private void SetConfigFloat(EntityUid uid, string key, float value) { GetConfig(uid).SetFloat(key, value); }
    private void SetConfigString(EntityUid uid, string key, string value) { GetConfig(uid).SetString(key, value); }
    private void SetConfigBool(EntityUid uid, string key, bool value) { GetConfig(uid).SetBool(key, value);  }

#endregion
        /// <summary>
        /// A helper function for use in ComponentInit event handlers.
        /// Ensures a BaseMechCompComponent exists and returns its config.
        /// </summary>
    private MechCompConfig EnsureConfig(EntityUid uid)
    {
        return EnsureComp<BaseMechCompComponent>(uid).config;
    }
    private bool isAnchored(EntityUid uid)  // perhaps i'm reinventing the wheel here
    {
        return TryComp<TransformComponent>(uid, out var comp) && comp.Anchored;
    }
    #region cooldown shite
    /// <summary>
    /// Convenience function for managing cooldowns for all devices.
    /// </summary>
    /// <returns>True if the cooldown period hasn't passed yet; false otherwise</returns>
    private bool IsOnCooldown(EntityUid uid, string key)
    {
        return !Cooldown(uid, key, null);
    }
    /// <summary>
    /// Convenience function for managing cooldowns for all mechcomp devices. If the previously set cooldown did not expire, does not set a new one and returns false.
    /// </summary>
    /// <returns>True if managed to set cooldown; false otherwise</returns>
    private bool Cooldown(EntityUid uid, string key, float seconds, Action? callback = null)
    {
        return Cooldown(uid, key, TimeSpan.FromSeconds(seconds), callback);
    }
    /// <summary>
    /// Convenience function for managing cooldowns for all mechcomp devices. If the previously set cooldown did not expire, does not set a new one and returns false.
    /// </summary>
    /// <returns>True if was able to set cooldown; false otherwise. If timespan is null, acts the same except won't actually set any cooldown.</returns>
    private bool Cooldown(EntityUid uid, string key, TimeSpan? timespan, Action? callback = null)
    {
        var tuple = (uid, key);
        if (!_timeSpans.TryGetValue(tuple, out var entry)) // || _timing.CurTime > entry.Item1)
        {
            if(timespan != null)
            {
                _timeSpans[tuple] = (_timing.CurTime + timespan.Value, callback);
            }
            return true;
        }
        return false;
    }
    /// <summary>
    /// Sets a cooldown regardless of whether or not it has passed yet.
    /// </summary>
    /// <param name="uid"></param> EntityUid for which we set the cooldown
    /// <param name="key"></param> Cooldown key. Multiple different cooldowns with different keys. can be set on the same EntityUid.
    /// <param name="seconds"></param>
    /// <param name="callback"></param> Delegate to execute after the cooldown expires.
    /// <param name="fastForwardLastCD"></param> If overwriting a cooldown with a new one, will run it's delegate if true.
    private void ForceCooldown(EntityUid uid, string key, float seconds, Action? callback = null, bool fastForwardLastCD = false)
    {
        ForceCooldown(uid, key, TimeSpan.FromSeconds(seconds), callback, fastForwardLastCD);
    }
    private void ForceCooldown(EntityUid uid, string key, TimeSpan timespan, Action? callback = null, bool fastForwardLastCD = false)
    {
        var tuple = (uid, key);
        if (fastForwardLastCD && _timeSpans.TryGetValue(tuple, out var entry) && entry.Item2 != null)
        {
            entry.Item2();
        }
        _timeSpans[tuple] = (_timing.CurTime + timespan, callback);
    }
    /// <summary>
    /// Cancel cooldown.
    /// </summary>
    /// <param name="uid"></param> EntityUid for which we cancel the cooldown
    /// <param name="key"></param> Cooldown key.
    /// <param name="fastForward"></param> Run the delegate if the cooldown has one.
    /// <returns>True if the cooldown existed and was removed; false otherwise.</returns>
    private bool CancelCooldown(EntityUid uid, string key, bool fastForward)
    {
        var tuple = (uid, key);
        if (fastForward && _timeSpans.TryGetValue(tuple, out var entry) && entry.Item2 != null)
        {
            entry.Item2();
        }
        return _timeSpans.Remove(tuple);
    }
#endregion
    private void OpenMechCompConfigDialog(EntityUid deviceUid, EntityUid playerUid, BaseMechCompComponent comp)
    {
        if(!Exists(deviceUid) || !Exists(playerUid))
        {
            return;
        }
        if(!_playerManager.TryGetSessionByEntity(playerUid, out var player))
        {
            return;
        }

        var config = comp!.config;
        var entries = config.GetOrdered();
        _dialog.OpenDialog(
            player,
            Name(deviceUid) + " configuration",
            entries, (results) => {
                config.SetFromObjectArray(results);
                RaiseLocalEvent(deviceUid, new MechCompConfigUpdateEvent());
            }
        );
    }
    private void SendMechCompSignal(EntityUid uid, string port, string signal, DeviceLinkSourceComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var data = new NetworkPayload
        {
            ["mechcomp_data"] = signal
        };
        //var data = new NetworkPayload();
        //data.Add("mechcomp_data", signal);
        _link.InvokePort(uid, port, data, comp);
    }
    private bool TryGetMechCompSignal(NetworkPayload? packet, out string signal)
    {
        //Logger.Debug($"TryGetMechCompSignal called ({packet?.ToString()}) ({packet != null}) ({packet?.TryGetValue<string>("mechcomp_data", out string? shit)})");

        if (packet != null && packet.TryGetValue<string>("mechcomp_data", out string? sig))
        {
            signal = sig;
            return true;
        }
        else
        {
            signal = "";
            return false;
        }
    }
    /// <summary>
    /// <see cref="SharedAppearanceSystem.SetData(EntityUid, Enum, object, AppearanceComponent?)"/>, but it forces
    /// the update by first setting the key value to a placeholder, and then to actual value. Used by stuff that hijacks
    /// the appearance system to send messages on when they're supposed to play animations n' shiet. (buttons, speakers)
    /// </summary>
    private void ForceSetData(EntityUid uid, Enum key, object value, AppearanceComponent? component = null)
    {
        object placeholder = 0xA55B1A57;
        if (value == placeholder) // what the fuck are you doing? 
            placeholder = 0x5318008;
        _appearance.SetData(uid, key, placeholder, component);
        _appearance.SetData(uid, key, value, component);

    }
    #endregion
    public override void Initialize()
    {
        SubscribeLocalEvent<BaseMechCompComponent, GetVerbsEvent<InteractionVerb>>(GetInteractionVerb);     // todo: currently BaseMechCompComponent handles config and
        SubscribeLocalEvent<BaseMechCompComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);          //       unanchoring stuff. Functional mechcomp components
        SubscribeLocalEvent<BaseMechCompComponent, DeviceLinkTryConnectingAttemptEvent>(OnConnectUIAttempt);  //       still process SignalReceivedEvents directly. Perhaps
        SubscribeLocalEvent<BaseMechCompComponent, LinkAttemptEvent>(OnConnectAttempt);  //       I should make some MechCompSignalReceived event and
                                                                                                                                                                                                              //       have them process that?
        SubscribeLocalEvent<MechCompButtonComponent, ComponentInit>(OnButtonInit);
        SubscribeLocalEvent<MechCompButtonComponent, InteractHandEvent>(OnButtonHandInteract);
        SubscribeLocalEvent<MechCompButtonComponent, ActivateInWorldEvent>(OnButtonActivation);
        

        SubscribeLocalEvent<MechCompSpeakerComponent, ComponentInit>(OnSpeakerInit);
        SubscribeLocalEvent<MechCompSpeakerComponent, MechCompConfigUpdateEvent>(OnSpeakerConfigUpdate);
        SubscribeLocalEvent<MechCompSpeakerComponent, SignalReceivedEvent>(OnSpeakerSignal);

        SubscribeLocalEvent<MechCompTeleportComponent, ComponentInit>(OnTeleportInit);
        SubscribeLocalEvent<MechCompTeleportComponent, SignalReceivedEvent>(OnTeleportSignal);

        SubscribeLocalEvent<MechCompMathComponent, ComponentInit>(OnMathInit);
        SubscribeLocalEvent<MechCompMathComponent, SignalReceivedEvent>(OnMathSignal);

        SubscribeLocalEvent<MechCompPressurePadComponent, ComponentInit>(OnPressurePadInit);
        SubscribeLocalEvent<MechCompPressurePadComponent, StepTriggeredEvent>(OnPressurePadStep);

        SubscribeLocalEvent<MechCompComparerComponent, ComponentInit>(OnComparerInit);
        SubscribeLocalEvent<MechCompComparerComponent, SignalReceivedEvent>(OnComparerSignal);

    }
    public override void Update(float frameTime)
    {
        List<(EntityUid, string)> keysToRemove = new();
        foreach(var kv in _timeSpans)
        {
            var key = kv.Key;
            var value = kv.Value;
            if(value.Item1 <= _timing.CurTime)
            {
                if(value.Item2 != null) { value.Item2(); }
                keysToRemove.Add(key);
            }
        }
        foreach(var key in keysToRemove)
        {
            _timeSpans.Remove(key);
        }
    }

    //private void OnButtonUnanchor(EntityUid uid, BaseMechCompComponent comp, AnchorStateChangedEvent args)
    //{
    //    throw new NotImplementedException();
    //}
    // These are shit names, i know.
    // This one is fired when opening network configurator's linking UI, and will be cancelled
    private void OnConnectUIAttempt(EntityUid uid, BaseMechCompComponent comp, DeviceLinkTryConnectingAttemptEvent args)
    {
        if (!isAnchored(uid))
        {
            _popup.PopupEntity(Loc.GetString("network-configurator-link-mode-cancelled-mechcomp-unanchored"), uid, args.User);
            args.Cancel();
        }
    }
    // These are shit names, i know.
    // This one is fired when 2 components are about to be connected, and will be cancelled if this component is unanchored.
    private void OnConnectAttempt(EntityUid uid, BaseMechCompComponent comp, LinkAttemptEvent args)
    {
        if (!isAnchored(uid))
        {
            if(args.User != null)
                _popup.PopupEntity(Loc.GetString("network-configurator-link-mode-cancelled-mechcomp-unanchored"), uid, args.User.Value);
            args.Cancel();
        }
    }

    private void OnAnchorStateChanged(EntityUid uid, BaseMechCompComponent comp, AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            if(HasComp<DeviceLinkSinkComponent>(uid))
                _link.RemoveAllFromSink(uid);
            if (HasComp<DeviceLinkSourceComponent>(uid))
                _link.RemoveAllFromSource(uid);
        }
        _appearance.SetData(uid, MechCompDeviceVisuals.Anchored, args.Anchored);
    }
    private void GetInteractionVerb(EntityUid uid, BaseMechCompComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!HasComp<NetworkConfiguratorComponent>(args.Using))
        {
            return;
        }

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("mechcomp-configure-device-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")), // placeholder
            Act = () => { OpenMechCompConfigDialog(uid, args.User, comp); }
        });
    }

    private void OnButtonInit(EntityUid uid, MechCompButtonComponent comp, ComponentInit args)
    {
        EnsureConfig(uid).Build(
            ("outsignal", (typeof(string), "Сигнал на выходе", "1"))
            );
        _link.EnsureSourcePorts(uid, "MechCompStandardOutput");
        
    }
    private void OnButtonHandInteract(EntityUid uid, MechCompButtonComponent comp, InteractHandEvent args)
    {
        ButtonClick(uid, comp);
    }
    private void OnButtonActivation(EntityUid uid, MechCompButtonComponent comp, ActivateInWorldEvent args)
    {
        ButtonClick(uid, comp);
    }
    private void ButtonClick(EntityUid uid, MechCompButtonComponent comp)
    {
        if (isAnchored(uid) && Cooldown(uid, "pressed", 1f))
        {
            _audio.PlayPvs(comp.ClickSound, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
            SendMechCompSignal(uid, "MechCompStandardOutput", GetConfigString(uid, "outsignal"));
            ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated"); // the data will be discarded anyways
        }
    }


    private void OnSpeakerInit(EntityUid uid, MechCompSpeakerComponent comp, ComponentInit args)
    {
        _link.EnsureSinkPorts(uid, "MechCompStandardInput");

        EnsureConfig(uid).Build(
            ("inradio", (typeof(bool), "Голосить в радио (;)", false )),
            ("name", (typeof(string), "Имя", Name(uid) ))
        );
        EnsureComp<VoiceMaskComponent>(uid, out var maskcomp);
        maskcomp.VoiceName = Name(uid); // better safe than █████ ███ ██████
    }
    private void OnSpeakerConfigUpdate(EntityUid uid, MechCompSpeakerComponent comp, MechCompConfigUpdateEvent args)
    {
        Comp<VoiceMaskComponent>(uid).VoiceName = GetConfigString(uid, "name");
    }
    private void OnSpeakerSignal(EntityUid uid, MechCompSpeakerComponent comp, ref SignalReceivedEvent args)
    {

        //Logger.Debug($"MechComp speaker received signal ({args.ToString()}) ({args.Data?.ToString()}) ({ToPrettyString(uid)})");
        if (isAnchored(uid) && TryGetMechCompSignal(args.Data, out string msg))
        {
            msg = msg.ToUpper();
            ForceSetData(uid, MechCompDeviceVisuals.Mode, "activated");
            //Logger.Debug($"MechComp speaker spoke ({msg}) ({ToPrettyString(uid)})");
            if (GetConfigBool(uid, "inradio") && Cooldown(uid, "speech", 5f))
            {
                
                _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true, checkRadioPrefix: false, nameOverride: GetConfigString(uid, "name"));
                _radio.SendRadioMessage(uid, msg, "Common", uid);
            }
            else if (Cooldown(uid, "speech", 1f)) {
                _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true, checkRadioPrefix: false, nameOverride: GetConfigString(uid, "name"));
            }
        }
    }



    private void OnTeleportInit(EntityUid uid, MechCompTeleportComponent comp, ComponentInit args)
    {
        EnsureConfig(uid).Build(
            ("TeleID", (typeof(Hex16), "ID этого телепорта", _rng.Next(65536))),
            ("_", (null, "Установите ID на 0000, чтобы отключить приём."))
        );
        _link.EnsureSinkPorts(uid, "MechCompTeleIDInput");
    }


    private void OnTeleportSignal(EntityUid uid, MechCompTeleportComponent comp, ref SignalReceivedEvent args)
    {
        if (IsOnCooldown(uid, "teleport")) {
            //_audio.PlayPvs("/Audio/White/MechComp/generic_energy_dryfire.ogg", uid);
            //return;
        }
        if (!TryGetMechCompSignal(args.Data, out string _sig) ||
            !int.TryParse(_sig, System.Globalization.NumberStyles.HexNumber, null, out int targetId) ||
            targetId == 0)
        {
            return;
        }
        

        TransformComponent? target = null;
        if (!TryComp<TransformComponent>(uid, out var telexform)) return;
        foreach(var (othercomp, otherbase, otherxform) in EntityQuery<MechCompTeleportComponent, BaseMechCompComponent, TransformComponent>())
        {
            var otherUid = othercomp.Owner;
            var distance = (_xform.GetWorldPosition(uid) - _xform.GetWorldPosition(otherUid)).Length();
            if (otherxform.Anchored && targetId == GetConfigInt(otherUid, "TeleID"))
            {
                if (distance <= comp.MaxDistance && distance <= othercomp.MaxDistance) // huh
                {
                    target = otherxform;
                    break;
                }
            }
        }
        if (target == null) {
            _audio.PlayPvs("/Audio/White/MechComp/generic_energy_dryfire.ogg", uid);
            Cooldown(uid, "teleport", 0.7f);
            return;
        }

        var targetUid = target.Owner;
        _appearance.SetData(uid, MechCompDeviceVisuals.Mode, "firing");
        _appearance.SetData(target.Owner, MechCompDeviceVisuals.Mode, "charging");

        // because the target tele has a cooldown of a second, it can be used to quickly move
        // back and make the original tele and reset it's cooldown down to a second.
        // i decided it would be fun to abuse, and thus, it will be left as is
        // if it turns out to be not fun, add check that newCooldown > currentCooldown
        ForceCooldown(uid, "teleport", 7f, () => { _appearance.SetData(uid, MechCompDeviceVisuals.Mode, "ready"); });
        ForceCooldown(targetUid, "teleport", 1f, () => { _appearance.SetData(target.Owner, MechCompDeviceVisuals.Mode, "ready"); });
        
        Spawn("EffectSparks", Transform(uid).Coordinates);
        Spawn("EffectSparks", Transform(targetUid).Coordinates);
        _audio.PlayPvs("/Audio/White/MechComp/emitter2.ogg", uid);
        _audio.PlayPvs("/Audio/White/MechComp/emitter2.ogg", targetUid);
        // var sol = new Solution();
        // sol.AddReagent("Water", 500f); // hue hue hue
        // _smoke.StartSmoke(uid, sol, 6f, 1);
        // sol = new Solution();
        // sol.AddReagent("Water", 500f); // hue hue hue
        // _smoke.StartSmoke(uid, sol, 6f, 1);

        foreach (EntityUid u in TurfHelpers.GetEntitiesInTile(telexform.Coordinates, LookupFlags.Uncontained))
        {
            if (TryComp<TransformComponent>(u, out var uxform) && !uxform.Anchored) {
                _xform.SetCoordinates(u, target.Coordinates);
            }
        }
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

    public void OnPressurePadInit(EntityUid uid, MechCompPressurePadComponent comp, ComponentInit args)
    {
        EnsureConfig(uid).Build(
            ("triggered_by_mobs", (typeof(bool), "Реагировать на существ", true) ),
            ("triggered_by_items", (typeof(bool), "Реагировать на предметы", false))
            );
        _link.EnsureSourcePorts(uid, "MechCompStandardOutput");
    }
    public void OnPressurePadStep(EntityUid uid, MechCompPressurePadComponent comp, ref StepTriggeredEvent args)
    {
        if (HasComp<MobStateComponent>(args.Tripper) && GetConfig(uid).GetBool("triggered_by_mobs"))
        {
            SendMechCompSignal(uid, "MechCompStandardOutput", Comp<MetaDataComponent>(args.Tripper).EntityName);
            return;
        }
        if (HasComp<ItemComponent>(args.Tripper) && GetConfig(uid).GetBool("triggered_by_items"))
        {
            SendMechCompSignal(uid, "MechCompStandardOutput", Comp<MetaDataComponent>(args.Tripper).EntityName);
            return;
        }
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
            ("_", (null,    "Режимы сравнения >, <, >=, <=")), // todo: check if newlines work
            ("__", (null,   "работают только с числовыми значениями."))
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
[RegisterComponent]
public partial class BaseMechCompComponent : Component
{
    public MechCompConfig config = new();


}
