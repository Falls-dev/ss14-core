using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Shared.E20;
using Content.Shared.Explosion.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.E20;

public partial class E20System : SharedE20System
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;

    private readonly List<EventsDelegate> _eventsList = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveTimerTriggerComponent, ComponentRemove>(OnTimerRemove);
        Events();
    }

    private void Events()
    {
        _eventsList.Add(FullDestructionEvent);
        _eventsList.Add(DieEvent);
        _eventsList.Add(AngryMobsSpawnEvent);
        _eventsList.Add(ItemsDestructionEvent);
        _eventsList.Add(MonkeyPolymorphEvent);
        _eventsList.Add(SpeedReduceEvent);
        _eventsList.Add(ThrowingEvent);
        _eventsList.Add(ExplosionEvent);
        _eventsList.Add(DiseaseEvent);
        _eventsList.Add(NothingEvent);
        _eventsList.Add(CookieEvent);
        _eventsList.Add(RejuvenateEvent);
        _eventsList.Add(MoneyEvent);
        _eventsList.Add(RevolverEvent);
        _eventsList.Add(MagicWandEvent);
        _eventsList.Add(SlaveEvent);
        _eventsList.Add(RandomSyndieBundleEvent);
        _eventsList.Add(FullAccessEvent);
        _eventsList.Add(DamageResistEvent);
        _eventsList.Add(ChangelingTransformationEvent);
    }

    private delegate void EventsDelegate(EntityUid uid, E20Component comp);

    private void E20Picker(EntityUid uid, E20Component comp)
    {
        ExplosionEvent(uid, comp);
        _polymorphSystem.PolymorphEntity(uid, "DiceShard");
    }

    private void DiceOfFatePicker(EntityUid uid, E20Component comp)
    {
        comp.IsUsed = true;

        if (comp.CurrentValue > 0 && comp.CurrentValue <= _eventsList.Count)
        {
            _eventsList[comp.CurrentValue - 1]?.Invoke(uid, comp);
        }

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

    /*protected override void Roll(EntityUid uid, E20Component? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        var roll = _random.Next(1, die.Sides + 1);
        SetCurrentSide(uid, roll, die);

        _popup.PopupEntity(Loc.GetString("dice-component-on-roll-land", ("die", uid), ("currentSide", die.CurrentValue)), uid);
        _audio.PlayPvs(die.Sound, uid);
    }*/
}

