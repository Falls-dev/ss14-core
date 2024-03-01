using Content.Server.Atmos.EntitySystems;
using Content.Shared._White.Wizard.ScrollSystem;

namespace Content.Server._White.Wizard.Scrolls;

public sealed class ScrollSystem : SharedScrollSystem
{
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;

    protected override void BurnScroll(EntityUid uid)
    {
        RemComp<ScrollComponent>(uid);

        _flammableSystem.Ignite(uid, uid);
    }
}
