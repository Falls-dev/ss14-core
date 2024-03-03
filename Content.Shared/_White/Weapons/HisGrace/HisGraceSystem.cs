using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._White.Weapons.HisGrace;

public abstract class SharedHisGraceSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly TimeSpan _updateFrequency = TimeSpan.FromSeconds(1);

    private const float HisGraceSatiated = 0;
    private const float HisGracePeckish = 20;
    private const float HisGraceHungry = 60;
    private const float HisGraceFamished = 100;
    private const float HisGraceStarving = 120;
    private const float HisGraceConsumeOwner = 140;
    private const float HisGraceFallAsleep = 160;
    private const string HisGraceMasterTag = "HisGraceMaster";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HisGraceComponent, ComponentInit>(OnHisGraceInit);
        SubscribeLocalEvent<HisGraceComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HisGraceComponent, MeleeHitEvent>(OnHisGraceHit);
        SubscribeLocalEvent<HisGraceComponent, GettingPickedUpAttemptEvent>(OnHisGracePickedUp);
        SubscribeLocalEvent<HisGraceComponent, DroppedEvent>(OnHisGraceDropped);
        SubscribeLocalEvent<HisGraceComponent, ComponentRemove>(OnHisGraceRemove);
    }

    private void OnHisGraceInit(EntityUid uid, HisGraceComponent hisGrace, ComponentInit args)
    {
        hisGrace.Container = _container.EnsureContainer<Container>(uid, "victims");
    }

    private void OnUseInHand(EntityUid uid, HisGraceComponent hisGrace, UseInHandEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        Awake(uid, hisGrace, args.User);

        args.Handled = true;
    }

    private void OnHisGraceDropped(EntityUid uid, HisGraceComponent component, DroppedEvent args)
    {
        BecomeNpc(uid);
    }

    private void OnHisGracePickedUp(EntityUid uid, HisGraceComponent hisGrace, GettingPickedUpAttemptEvent args)
    {
        if (hisGrace.Thirst < HisGraceConsumeOwner && _tag.HasTag(args.User, HisGraceMasterTag))
        {
            RemoveNpc(uid);
        }
    }

    private void OnHisGraceHit(EntityUid uid, HisGraceComponent hisGrace, MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            // if not humanoid or is not in critical/dead
            if (!TryComp<HumanoidAppearanceComponent>(target, out _) ||
                !_mobState.IsIncapacitated(target))
            {
                continue;
            }

            Consume(uid, hisGrace, target);
        }
    }

    private void OnHisGraceRemove(EntityUid uid, HisGraceComponent hisGrace, ComponentRemove args)
    {
        _container.EmptyContainer(hisGrace.Container);
    }

    private void Awake(EntityUid uid, HisGraceComponent hisGrace, EntityUid user) // Good morning, Mr. Grace
    {
        if (hisGrace.Awakened)
        {
            return;
        }

        _metaData.SetEntityName(uid, Loc.GetString("his-grace-awaken-name"));
        _metaData.SetEntityDescription(uid, Loc.GetString("his-grace-awaken-description"));

        _popup.PopupEntity(Loc.GetString("his-grace-awakes", ("HisGrace", uid)), uid, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("his-grace-awakes-user", ("HisGrace", uid)), uid, user, PopupType.Medium);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/pope_entry.ogg", AudioParams.Default.WithVolume(-3)),
            uid);

        hisGrace.Awakened = true;
        hisGrace.Master = user;
        _tag.AddTag(user, HisGraceMasterTag);
    }

    private void Drowse(EntityUid uid, HisGraceComponent hisGrace) // Good night, Mr. Grace
    {
        if (!hisGrace.Awakened || hisGrace.Ascended)
        {
            return;
        }

        _metaData.SetEntityName(uid, Loc.GetString("his-grace-drowse-name"));
        _metaData.SetEntityDescription(uid, Loc.GetString("his-grace-drowse-description"));

        hisGrace.Awakened = false;
        hisGrace.Thirst = 0;
        hisGrace.Master = null;

        _popup.PopupEntity(Loc.GetString("his-grace-falls-asleep", ("HisGrace", uid)), uid, PopupType.MediumCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/batonextend.ogg"), uid);

        var meleeWeapon = Comp<MeleeWeaponComponent>(uid);
        meleeWeapon.Damage.DamageDict["Blunt"] -= hisGrace.CurrentVictims * hisGrace.DamagerPerVictim;

        hisGrace.CurrentVictims = 0;
    }

    private void Consume(EntityUid hisGraceUid, HisGraceComponent hisGrace, EntityUid target)
    {
        if (target == hisGrace.Master)
        {
            hisGrace.Master = null;
        }

        AdjustBloodthirst(hisGraceUid, hisGrace, -5);
        _container.Insert(target, hisGrace.Container);

        if (!_mind.TryGetMind(target, out _, out _))
        {
            return;
        }

        hisGrace.CurrentVictims++;

        var meleeComp = Comp<MeleeWeaponComponent>(hisGraceUid);
        meleeComp.Damage.DamageDict["Blunt"] += hisGrace.DamagerPerVictim;

        if (hisGrace.CurrentVictims > hisGrace.VictimsNeeded)
        {
            Ascend(hisGraceUid, hisGrace);
        }
    }

    protected virtual void Ascend(EntityUid hisGraceUid, HisGraceComponent hisGrace)
    {
        if (hisGrace.Ascended || hisGrace.Master is null)
        {
            return;
        }

        hisGrace.Ascended = true;
        hisGrace.Thirst = 130;

        _metaData.SetEntityDescription(hisGraceUid, Loc.GetString("his-grace-ascended-description"));

        if (_mind.TryGetMind(hisGrace.Master!.Value, out _, out _))
        {
            _metaData.SetEntityName(hisGraceUid, Loc.GetString("his-grace-ascended-name", ("master", hisGrace.Master)));
        }
    }

    private void AdjustBloodthirst(EntityUid hisGraceUid, HisGraceComponent hisGrace, float amount)
    {
        if (hisGrace.Ascended)
        {
            return;
        }

        var previousThirst = hisGrace.Thirst;
        hisGrace.Thirst = hisGrace.Thirst < HisGraceConsumeOwner
            ? Math.Clamp(hisGrace.Thirst + amount, HisGraceSatiated, HisGraceConsumeOwner)
            : Math.Clamp(hisGrace.Thirst + amount, HisGraceConsumeOwner, HisGraceFallAsleep);

        UpdateStats(hisGraceUid, hisGrace, previousThirst);
    }

    private void UpdateStats(EntityUid hisGraceUid, HisGraceComponent hisGrace, float previousThirst)
    {
        // God bless shitcode
        switch (hisGrace.Thirst)
        {
            case > HisGraceFallAsleep:
            {
                Drowse(hisGraceUid, hisGrace);
                break;
            }
            case > HisGraceConsumeOwner and < HisGraceFallAsleep:
            {
                if (HisGraceConsumeOwner > previousThirst)
                {
                    PopupHisGrace("his-grace-frenzy", hisGraceUid);
                    RemComp<UnremoveableComponent>(hisGraceUid);
                    BecomeNpc(hisGraceUid);
                    _tag.RemoveTag(hisGrace.Master!.Value, HisGraceMasterTag);
                }

                break;
            }
            case > HisGraceStarving and < HisGraceConsumeOwner:
            {
                if (HisGraceStarving > previousThirst)
                {
                    PopupHisGrace("his-grace-starving", hisGraceUid);
                }

                break;
            }
            case > HisGraceFamished and < HisGraceStarving:
            {
                switch (previousThirst)
                {
                    case < HisGraceFamished:
                        PopupHisGrace("his-grace-famished", hisGraceUid);
                        break;
                    case >= HisGraceStarving:
                        PopupHisGrace("his-grace-thirst-decresed", hisGraceUid);
                        break;
                }

                break;
            }
            case > HisGraceHungry and < HisGraceFamished:
            {
                switch (previousThirst)
                {
                    case < HisGraceHungry:
                        PopupHisGrace("his-grace-hungry", hisGraceUid);
                        AddComp<UnremoveableComponent>(hisGraceUid);
                        break;
                    case >= HisGraceFamished:
                        PopupHisGrace("his-grace-thirst-decresed", hisGraceUid);
                        break;
                }

                break;
            }
            case > HisGracePeckish and < HisGraceHungry:
            {
                switch (previousThirst)
                {
                    case < HisGracePeckish:
                        PopupHisGrace("his-grace-peckish", hisGraceUid);
                        break;
                    case >= HisGraceHungry:
                        RemComp<UnremoveableComponent>(hisGraceUid);
                        PopupHisGrace("his-grace-thirst-decresed", hisGraceUid);
                        break;
                }

                break;
            }
            case > HisGraceSatiated and < HisGracePeckish:
            {
                if (previousThirst >= HisGracePeckish)
                {
                    PopupHisGrace("his-grace-thirst-decresed", hisGraceUid);
                }

                break;
            }
        }
    }

    protected virtual void BecomeNpc(EntityUid target)
    {
    }

    protected virtual void RemoveNpc(EntityUid target)
    {
    }

    private void PopupHisGrace(string locId, EntityUid uid)
    {
        _popup.PopupEntity(Loc.GetString(locId, ("HisGrace", uid)), uid, PopupType.MediumCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HisGraceComponent>();

        while (query.MoveNext(out var uid, out var hisGrace))
        {
            if (hisGrace.Ascended || !hisGrace.Awakened)
            {
                continue;
            }

            if (_gameTiming.CurTime < hisGrace.NextUpdateTime)
            {
                continue;
            }

            hisGrace.NextUpdateTime += _updateFrequency;

            if (hisGrace.Thirst < HisGraceConsumeOwner)
            {
                AdjustBloodthirst(uid, hisGrace,
                    (float) (1 + Math.Min(Math.Floor(hisGrace.CurrentVictims / 5f), 2)) / 3f);
            }
            else
            {
                AdjustBloodthirst(uid, hisGrace, 1 / 3f);
            }
        }
    }
}