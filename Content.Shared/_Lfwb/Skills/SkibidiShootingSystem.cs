using Content.Shared.Hands;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;

namespace Content.Shared._Lfwb.Skills;

public sealed class SkibidiShootingSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedSkillsSystem _skillsSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnDopDop);
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnYesYes);
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnSkibidi);
        SubscribeLocalEvent<GunComponent, ComponentInit>(OnWtfNigga);
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnShoot);
    }

    private void OnShoot(Entity<GunComponent> ent, ref GunShotEvent args)
    {
        if (_netManager.IsServer)
            _skillsSystem.ApplySkillThreshold(args.User, Skill.Ranged, 5);
    }

    private void OnWtfNigga(EntityUid uid, GunComponent component, ComponentInit args)
    {
        component.OldMinAngle = component.MinAngle;
        component.OldMaxAngle = component.MaxAngle;
        component.OldAngleDecay = component.AngleDecay;
        component.OldAngleIncrease = component.AngleIncrease;
        component.OldFireRate = component.FireRate;

        Dirty(uid, component);
    }

    private void OnDopDop(EntityUid uid, GunComponent component, GotEquippedHandEvent args)
    {
        component.CurrentShooter = args.User;
        _gunSystem.RefreshModifiers(uid);
    }

    private void OnYesYes(EntityUid uid, GunComponent component, GotUnequippedHandEvent args)
    {
        component.CurrentShooter = null;
        ResetNiggaGun(uid, component);
        _gunSystem.RefreshModifiers(uid);
    }

    private void OnSkibidi(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (ent.Comp.CurrentShooter == null)
            return;

        var skillLevel = _skillsSystem.GetSkillLevel(ent.Comp.CurrentShooter.Value, Skill.Ranged);
        args.FireRate += _skillsSystem.SkillLevelToSkibidi[skillLevel];

        // Ебануть что нибудь чтобы сложнее было стрелять я хз.
    }

    private void ResetNiggaGun(EntityUid gun, GunComponent component)
    {
        component.MinAngle = component.OldMinAngle;
        component.MaxAngle = component.OldMaxAngle;
        component.AngleDecay = component.OldAngleDecay;
        component.AngleIncrease = component.OldAngleIncrease;
        component.FireRate = component.OldFireRate;

        Dirty(gun, component);
    }
}
