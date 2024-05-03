using System.Reflection;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Shared.CCVar;
using Content.Shared.E20;
using Content.Shared.Explosion.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.E20;


public sealed class E20System : SharedE20System
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly E20SystemEvents _events = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveTimerTriggerComponent, ComponentRemove>(OnTimerRemove);
    }

    private delegate void EventsDelegate(EntityUid uid, E20Component comp);

    private void E20Picker(EntityUid uid, E20Component comp)
    {
        _events.ExplosionEvent(uid, comp);
        _polymorphSystem.PolymorphEntity(uid, "DiceShard");
    }

    private void DiceOfFatePicker(EntityUid uid, E20Component comp)
    {
        comp.IsUsed = true;
        //FIXME
        Dictionary<int, EventsDelegate> events = new Dictionary<int, EventsDelegate>();
        events[1] = _events.FullDestructionEvent;
        events[2] = _events.DieEvent;
        events[3] = _events.AngryMobsSpawnEvent;
        events[4] = _events.ItemsDestructionEvent;
        events[5] = _events.MonkeyPolymorphEvent;
        events[6] = _events.SpeedReduceEvent;
        events[7] = _events.ThrowingEvent;
        events[8] = _events.ExplosionEvent;
        events[9] = _events.DiseaseEvent;
        events[10] = _events.NothingEvent;
        events[11] = _events.CookieEvent;
        events[12] = _events.RejuvenateEvent;
        events[13] = _events.MoneyEvent;
        events[14] = _events.RevolverEvent;
        events[15] = _events.MagicWandEvent;
        events[16] = _events.SlaveEvent;
        events[17] = _events.RandomSyndieBundleEvent;
        events[18] = _events.FullAccessEvent;
        events[19] = _events.DamageResistEvent;
        events[20] = _events.ChangelingTransformationEvent;


        EventsDelegate method = events[comp.CurrentValue];
        method(uid, comp);

        if (comp.IsUsed)
        {
            _polymorphSystem.PolymorphEntity(uid, "DiceShard");
        }

    }

    private void OnTimerRemove(EntityUid uid, ActiveTimerTriggerComponent comp, ComponentRemove args)
    {
        if (!HasComp<E20Component>(uid))
        {
            return;
        }

        E20Component e20 = EntityManager.GetComponent<E20Component>(uid);


        if (e20.DiceType == "E20")
        {
            E20Picker(uid, e20);
            return;
        }

        DiceOfFatePicker(uid, e20);
    }

    protected override void TimerEvent(EntityUid uid, E20Component? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        _triggerSystem.HandleTimerTrigger(uid, uid, die.Delay, 1, 0, die.Beep);

        if (!((die.DiceType == "E20") & ((die.CurrentValue == 1) | (die.CurrentValue == 20))))
            return;

        _audio.PlayPvs(die.SoundDie, uid);
        _chat.TrySendInGameICMessage(uid, Loc.GetString("DIE"), InGameICChatType.Speak, true);
    }

    protected override void Roll(EntityUid uid, E20Component? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        var roll = _random.Next(1, die.Sides + 1);
        SetCurrentSide(uid, roll, die);

        _popup.PopupEntity(Loc.GetString("dice-component-on-roll-land", ("die", uid), ("currentSide", die.CurrentValue)), uid);
        _audio.PlayPvs(die.Sound, uid);
    }

}

