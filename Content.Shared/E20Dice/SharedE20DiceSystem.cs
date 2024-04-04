using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;

namespace Content.Shared.E20Dice;

public class SharedE20DiceSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<E20DiceComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<E20DiceComponent, LandEvent>(OnLand);
    }

    private void OnUseInHand(EntityUid uid, E20DiceComponent component, UseInHandEvent args)
    {
        Roll(uid, component);
        ExplosionEvent(uid, component);
        //EventPicker();

    }

    private void OnLand(EntityUid uid, E20DiceComponent component, LandEvent args)
    {
        Roll(uid, component);
        ExplosionEvent(uid, component);
    }

    public void SetCurrentSide(EntityUid uid, int side, E20DiceComponent? die = null)
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

    protected virtual void UpdateVisuals(EntityUid uid, E20DiceComponent? die = null)
    {
        // See client system.
    }

    public virtual void Roll(EntityUid uid, E20DiceComponent? die = null)
    {
        // See the server system, client cannot predict rolling.
    }

    public virtual void EventPicker(EntityUid uid, E20DiceComponent? die = null)
    {

    }

    public virtual void ExplosionEvent(EntityUid uid, E20DiceComponent? die = null)
    {
        // Privet kak dela my friend
    }
}
