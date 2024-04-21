using System.Reflection;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.E20;
using Content.Shared.Explosion.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.E20;

namespace Content.Server.E20;


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
    [Dependency] private readonly E20SystemEvents _events = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveTimerTriggerComponent, ComponentRemove>(OnTimerRemove);
    }

    private void OnTimerRemove(EntityUid uid, ActiveTimerTriggerComponent comp, ComponentRemove args)
    {
        //IoCManager.InjectDependencies(this);
        E20Component e20 = EntityManager.GetComponent<E20Component>(uid);
        E20SystemEvents eve = new E20SystemEvents();
        Type type = eve.GetType();
        //IoCManager.InjectDependencies(this);
        if (type != null)
        {
            object instance = Activator.CreateInstance(type)!;
            MethodInfo method = type.GetMethod(e20.Events[0], BindingFlags.Instance|BindingFlags.Public)!;
            object[] parameters = new object[] { uid, e20 };
            method.Invoke(instance, parameters);
        }


        /*Type myType = typeof(Content.Server.E20.E20SystemEvents);
        MethodInfo? methodInfo = myType.GetMethod(e20.Events[0], BindingFlags.Instance|BindingFlags.Public);
        object? instance = Activator.CreateInstance(myType);
        Console.WriteLine($"Event name: {e20.Events[0]}");
        Console.WriteLine($"Type name: {myType.FullName}");
        Console.WriteLine($"Method info: {methodInfo}");


        if (methodInfo != null)
        {
            object[] parameters = new object[] { uid, e20 };
            methodInfo?.Invoke(instance, parameters);
        }*/


        /*
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

        _explosion.TriggerExplosive(uid, totalIntensity:intensity);*/
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

