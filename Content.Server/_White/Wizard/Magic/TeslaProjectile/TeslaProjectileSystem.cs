using Content.Server.Lightning;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Server._White.Wizard.Magic.TeslaProjectile;

public sealed class TeslaProjectileSystem : EntitySystem
{
    [Dependency] private readonly LightningSystem _lightning = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaProjectileComponent, ProjectileHitEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<TeslaProjectileComponent> ent, ref ProjectileHitEvent args)
    {
       _lightning.ShootRandomLightnings(ent, 4, 8, arcDepth:4);
    }
}
