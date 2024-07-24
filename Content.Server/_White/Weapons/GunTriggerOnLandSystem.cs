using Content.Server._White.Explosion;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server._White.Weapons;

public sealed class GunTriggerOnLandSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GunTriggerOnLandComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void OnAmmoShot(EntityUid uid, GunTriggerOnLandComponent component, ref AmmoShotEvent args)
    {
        foreach (var ammo in args.FiredProjectiles)
        {
            EnsureComp<TriggerOnLandComponent>(ammo);
        }
    }
}
