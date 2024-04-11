using Content.Shared.E20;
using Robust.Client.GameObjects;

namespace Content.Client.E20;

public sealed class E20System : SharedE20System
{
    protected override void UpdateVisuals(EntityUid uid, E20Component? die = null)
    {
        if (!Resolve(uid, ref die) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        // TODO maybe just move each diue to its own RSI?
        var state = sprite.LayerGetState(0).Name;
        if (state == null)
            return;

        var prefix = state.Substring(0, state.IndexOf('_'));
        sprite.LayerSetState(0, $"{prefix}_{die.CurrentValue}");
    }
}
