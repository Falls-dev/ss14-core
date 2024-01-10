using System.Text;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Paper;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.White.VoiceRecorder;

/// <summary>
/// This handles the voice recorder work.
/// </summary>
public sealed class VoiceRecorderSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceRecorderComponent, ActivateInWorldEvent>(OnActivate);
        // SubscribeLocalEvent<VoiceRecorderComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<VoiceRecorderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VoiceRecorderComponent, ListenEvent>(SaveEntityMessage);
        SubscribeLocalEvent<VoiceRecorderComponent, ListenAttemptEvent>(CanListen);
        SubscribeLocalEvent<VoiceRecorderComponent, GetVerbsEvent<AlternativeVerb>>(AddVerbs);
    }

    public void OnInit(EntityUid uid, VoiceRecorderComponent component, ComponentInit args)
    {
        if (!TryComp<ActiveListenerComponent>(uid, out var listener))
        {
            RemComp<ActiveListenerComponent>(uid);
            component.Listening = false;
            return;
        }
    }

    public void OnActivate(EntityUid uid, VoiceRecorderComponent component, ActivateInWorldEvent args)
    {
        component.Listening = !component.Listening;
        ToggleListening(uid, component, component.Listening);
        var message = Loc.GetString(component.Listening ? "voice-recorder-on" : "voice-recorder-off");
        _popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void AddVerbs(EntityUid uid, VoiceRecorderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !component.Enabled || component.Listening || component.Recordings.Count == 0)
            return;
        AlternativeVerb verb = new();
        verb.Text = Loc.GetString("voice-recorder-print");
        verb.Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"));
        verb.Act = () => OnPrint(uid, component, component.Recordings.ToArray(), args.User);
        args.Verbs.Add(verb);
    }

    public void ToggleListening(EntityUid uid, VoiceRecorderComponent component, bool listening)
    {
        component.Listening = listening;
        if (listening)
        {
            component.Recordings.Clear();
            EnsureComp<ActiveListenerComponent>(uid).Range = component.Range;
        }
        else
        {
            RemComp<ActiveListenerComponent>(uid);
        }
        if (TryComp<AppearanceComponent>(uid, out var appearance) &&
             TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, listening ? "on" : "off", false, item);
            _appearance.SetData(uid, ToggleVisuals.Toggled, listening, appearance);
        }
    }

    public void CanListen(EntityUid uid, VoiceRecorderComponent component, ListenAttemptEvent args)
    {
        if (component.Blacklist.IsValid(args.Source))
            args.Cancel();
    }

    public void SaveEntityMessage(EntityUid uid, VoiceRecorderComponent component, ListenEvent args)
    {
        if (!component.Listening)
            return;

        component.Recordings.Add($"{Name(args.Source)}: {args.Message}");
        if (component.Recordings.Count > (component.MaximumEntries - 1))
        {
            ToggleListening(uid, component, false);
        }
    }

    public void OnPrint(EntityUid uid, VoiceRecorderComponent component, string[] messages, EntityUid user)
    {
        // TEXT TO PRINT
        var text = new StringBuilder();
        text.AppendLine(component.CustomTitle == "" ? Loc.GetString("voice-recorder-title") : component.CustomTitle);
        text.AppendLine("");
        text.AppendLine(Loc.GetString("voice-recorder-start"));
        foreach (var message in messages)
        {
            text.AppendLine(message);
        }
        text.AppendLine(Loc.GetString("voice-recorder-end"));

        var printed = EntityManager.SpawnEntity(component.MachineOutput, Transform(uid).Coordinates);
        _paperSystem.SetContent(printed, text.ToString());
        var stamp = new StampDisplayInfo();
        stamp.StampedName = Loc.GetString("voice-recorder-stamp");
        stamp.StampedColor = new Color(47, 47, 56);
        _paperSystem.TryStamp(printed, stamp, "paper_stamp-generic", null);
        _handsSystem.PickupOrDrop(user, printed, checkActionBlocker: false);
        _audioSystem.PlayPvs(component.SoundPrint, uid,
            AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));
        _metaData.SetEntityName(printed, Loc.GetString("voice-recorder-paper-name"));
        _metaData.SetEntityDescription(printed, Loc.GetString("voice-recorder-paper-desc"));
        ToggleListening(uid, component, false);
    }
}
