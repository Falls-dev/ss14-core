using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;

namespace Content.Shared.E20;

public abstract class SharedE20System : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<E20Component, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<E20Component, LandEvent>(OnLand);
        SubscribeLocalEvent<E20Component, AfterAutoHandleStateEvent>(OnDiceAfterHandleState);
    }

    private void OnDiceAfterHandleState(EntityUid uid, E20Component component, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(uid, component);
    }

    private void OnUseInHand(EntityUid uid, E20Component component , UseInHandEvent args)
    {
        if (component.IsActivated)
        {
            return;
        }

        Roll(uid, component);
        component.IsActivated = true;
        component.LastUser = args.User;
        TimerEvent(uid, component);
    }

    private void OnLand(EntityUid uid, E20Component component, ref LandEvent args)
    {
        if (component.IsActivated)
        {
            return;
        }

        Roll(uid, component);
    }

    protected void SetCurrentSide(EntityUid uid, int side, E20Component? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        if (side < 1 || side > die.Sides)
        {
            Log.Error($"Attempted to set die {ToPrettyString(uid)} to an invalid side ({side}).");
            return;
        }

        die.CurrentValue = (side - die.Offset) * die.Multiplier;
        Dirty(die);
        UpdateVisuals(uid, die);
    }

    protected virtual void UpdateVisuals(EntityUid uid, E20Component? die = null)
    {
        // See client system.
    }

    protected virtual void TimerEvent(EntityUid uid, E20Component? die = null)
    {
        // See the server system, client cannot count ¯\_(ツ)_/¯.
    }

    protected virtual void Roll(EntityUid uid, E20Component? die = null)
    {
        // See the server system, client cannot predict rolling.
    }


}
