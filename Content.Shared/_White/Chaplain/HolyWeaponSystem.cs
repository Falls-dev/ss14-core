using Content.Shared.Examine;

namespace Content.Shared._White.Chaplain;

public sealed class HolyWeaponSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyWeaponComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<HolyWeaponComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup("[color=lightblue]Данное оружие наделено священной силой.[/color]");
    }
}
