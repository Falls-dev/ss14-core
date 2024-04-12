using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion.Components;
using Robust.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.E20;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;


namespace Content.Server.E20Dice;


public sealed class E20System : SharedE20System
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveTimerTriggerComponent, ComponentRemove>(OnTimerRemove);
    }

    private void OnTimerRemove(EntityUid uid, ActiveTimerTriggerComponent comp, ComponentRemove args)
    {
        E20Component e20 = EntityManager.GetComponent<E20Component>(uid);
        float intensity = e20.CurrentValue * 280; // Calculating power of explosion

        if (e20.CurrentValue == 20) // Critmass-like explosion
        {
            _explosion.TriggerExplosive(uid, totalIntensity:intensity*15, radius:_cfgManager.GetCVar(CCVars.ExplosionMaxArea));
            return;
        }

        if (e20.CurrentValue == 1)
        {
            MapCoordinates coords = Transform(e20.LastUser).MapPosition;

            _explosion.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                4,1,2,0); // Small explosion for the sake of appearance
            _bodySystem.GibBody(e20.LastUser, true); // gibOrgans=true dont gibs the organs
            return;
        }

        _explosion.TriggerExplosive(uid, totalIntensity:intensity);
    }

    protected override void TimerEvent(EntityUid uid, E20Component? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        _triggerSystem.HandleTimerTrigger(uid, uid, die.Delay, 1, 0, die.Beep);

        if (!((die.CurrentValue == 1) | (die.CurrentValue == 20)))
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

