using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.E20;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.E20;

public sealed class E20SystemEvents : EntitySystem
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
        //SubscribeLocalEvent<E20Component, ExaminedEvent>(DieEvent);
        //IoCManager.Resolve<BodySystem>();

    }

    public void ExplosionEvent(EntityUid uid, E20Component comp)
    {
        float intensity = comp.CurrentValue * 280; // Calculating power of explosion


        if (comp.CurrentValue == 20) // Critmass-like explosion
        {
            _explosion.TriggerExplosive(uid, totalIntensity:intensity*15, radius:_cfgManager.GetCVar(CCVars.ExplosionMaxArea));
            return;
        }

        if (comp.CurrentValue == 1)
        {
            TransformSystem ts = new TransformSystem();
            MapCoordinates coords = ts.GetMapCoordinates(comp.LastUser);
            //MapCoordinates coords = Transform(comp.LastUser).MapPosition;

            _explosion.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                4,1,2,0); // Small explosion for the sake of appearance
            _bodySystem.GibBody(comp.LastUser, true); // gibOrgans=true dont gibs the organs
            return;
        }

        _explosion.TriggerExplosive(uid, totalIntensity:intensity);
    }

    private void DieEvent(EntityUid uid, E20Component comp, ExaminedEvent args)
    {

    }
}
