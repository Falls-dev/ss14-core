using Content.Server.Hands.Systems;
using Content.Shared._White.Chaplain;

namespace Content.Server._White.Chaplain;

public sealed class HolyWeaponSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NullRodComponent, WeaponSelectedEvent>(OnWeaponSelected);
    }

    private void OnWeaponSelected(Entity<NullRodComponent> ent, ref WeaponSelectedEvent args)
    {
        var entity = args.Session.AttachedEntity;

        if (args.SelectedWeapon == string.Empty || entity == null)
            return;

        var weapon = Spawn(args.SelectedWeapon, Transform(entity.Value).Coordinates);
        EnsureComp<HolyWeaponComponent>(weapon);

        Del(ent);

        _hands.PickupOrDrop(entity.Value, weapon, true, false, false);
    }
}
