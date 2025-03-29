using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Executions;

public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    private const float MeleeExecutionTimeModifier = 5.0f;
    private const float GunExecutionTime = 6.0f;
    private const float DamageModifier = 9.0f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecutionComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsMelee);
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);

        SubscribeLocalEvent<ExecutionComponent, ExecutionDoAfterEvent>(OnDoafterMelee);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);
    }

    private void OnGetInteractionVerbsMelee(
        EntityUid uid,
        ExecutionComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartMeleeExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void OnGetInteractionVerbsGun(
        EntityUid uid,
        GunComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartGunExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool CanExecuteWithAny(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!TryComp<DamageableComponent>(victim, out var damage))
            return false;

        if (!TryComp<MobStateComponent>(victim, out var mobState))
            return false;

        if (_mobStateSystem.IsDead(victim, mobState))
            return false;

        if (!_actionBlockerSystem.CanAttack(attacker, victim))
            return false;

        if (victim != attacker && _actionBlockerSystem.CanInteract(victim, null))
            return false;

        return true;
    }

    private bool CanExecuteWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(weapon, victim, user))
            return false;

        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return false;

        return true;
    }

    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(weapon, victim, user)) return false;

        if (!TryComp<GunComponent>(weapon, out var gun) && _gunSystem.CanShoot(gun!))
            return false;

        return true;
    }

    private void TryStartMeleeExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        var executionTime = (1.0f / Comp<MeleeWeaponComponent>(weapon).AttackRate) * MeleeExecutionTimeModifier;

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-melee-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-melee-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-melee-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-melee-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, executionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnHandChange = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void TryStartGunExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-gun-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-gun-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-gun-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-gun-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, GunExecutionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnHandChange = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnDoafterMelee(EntityUid uid, ExecutionComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var victim = args.Target!.Value;
        var weapon = args.Used!.Value;

        if (!CanExecuteWithMelee(weapon, victim, attacker)) return;

        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return;

        _damageableSystem.TryChangeDamage(victim, melee.Damage * DamageModifier, true);
        _audioSystem.PlayEntity(melee.HitSound, Filter.Pvs(weapon), weapon, true, AudioParams.Default);

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-melee-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-melee-complete-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-melee-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-melee-complete-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
    }

    // TODO: This repeats a lot of the code of the serverside GunSystem, make it not do that
    private void OnDoafterGun(EntityUid uid, GunComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var weapon = args.Used!.Value;
        var victim = args.Target!.Value;

        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        var prevention = new ShotAttemptedEvent
        {
            User = attacker,
            Used = weapon!
        };

        RaiseLocalEvent(weapon, ref prevention);
        if (prevention.Cancelled)
            return;

        RaiseLocalEvent(attacker, ref prevention);
        if (prevention.Cancelled)
            return;

        var attemptEv = new AttemptShootEvent(attacker, null);
        RaiseLocalEvent(weapon, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
            {
                _popupSystem.PopupClient(attemptEv.Message, weapon, attacker);
                return;
            }
        }

        var fromCoordinates = Transform(attacker).Coordinates;
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, attacker);
        RaiseLocalEvent(weapon, ev);

        if (ev.Ammo.Count <= 0)
        {
            _audioSystem.PlayEntity(component.SoundEmpty, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
            ShowExecutionPopup("execution-popup-gun-empty", Filter.Pvs(weapon), PopupType.Medium, attacker, victim, weapon);
            return;
        }

        var damage = new DamageSpecifier();

        var ammoUid = ev.Ammo[0].Entity;
        switch (ev.Ammo[0].Shootable)
        {
            case CartridgeAmmoComponent cartridge:
                var prototype = _prototypeManager.Index<EntityPrototype>(cartridge.Prototype);
                prototype.TryGetComponent<ProjectileComponent>(out var projectileA, _componentFactory);
                if (projectileA != null)
                {
                    damage = projectileA.Damage * cartridge.Count;
                }

                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                Dirty(ammoUid.Value, cartridge);

                break;

            case AmmoComponent newAmmo:
                TryComp<ProjectileComponent>(ammoUid, out var projectileB);
                if (projectileB != null)
                {
                    damage = projectileB.Damage;
                }
                Del(ammoUid);
                break;

            case HitscanPrototype hitscan:
                damage = hitscan.Damage!;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (TryComp<ClumsyComponent>(attacker, out var clumsy) && component.ClumsyProof == false)
        {
            if (_interactionSystem.TryRollClumsy(attacker, 0.33333333f, clumsy))
            {
                ShowExecutionPopup("execution-popup-gun-clumsy-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
                ShowExecutionPopup("execution-popup-gun-clumsy-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);

                _damageableSystem.TryChangeDamage(attacker, damage, origin: attacker);
                _audioSystem.PlayEntity(component.SoundGunshot, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
                return;
            }
        }

        _damageableSystem.TryChangeDamage(victim, damage * DamageModifier, true);
        _audioSystem.PlayEntity(component.SoundGunshot, Filter.Pvs(weapon), weapon, false, AudioParams.Default);

        if (attacker != victim)
        {
            ShowExecutionPopup("execution-popup-gun-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-gun-complete-external", Filter.PvsExcept(attacker), PopupType.LargeCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("suicide-popup-gun-complete-internal", Filter.Entities(attacker), PopupType.LargeCaution, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-gun-complete-external", Filter.PvsExcept(attacker), PopupType.LargeCaution, attacker, victim, weapon);
        }
    }

    private void ShowExecutionPopup(string locString,
        Filter filter,
        PopupType type,
        EntityUid attacker,
        EntityUid victim,
        EntityUid weapon)
    {
        _popupSystem.PopupEntity(Loc.GetString(
                locString,
                ("attacker", attacker),
                ("victim", victim),
                ("weapon", weapon)),
            attacker,
            filter,
            true,
            type);
    }
}
