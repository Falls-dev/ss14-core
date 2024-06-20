using System.Linq;
using System.Numerics;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Spawners;

namespace Content.Shared._White.Wizard.Timestop;

public sealed class FreezeContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FreezeContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FreezeContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<FrozenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FrozenComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<FrozenComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<FrozenComponent, EntGotInsertedIntoContainerMessage>(OnGetInserted);
    }

    private void OnGetInserted(Entity<FrozenComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        RemCompDeferred<FrozenComponent>(ent);
    }

    private void OnPreventCollide(Entity<FrozenComponent> ent, ref PreventCollideEvent args)
    {
        if (args.OurBody.BodyType == BodyType.Dynamic && !HasComp<FreezeContactsComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnRemove(Entity<FrozenComponent> ent, ref ComponentRemove args)
    {
        var (uid, comp) = ent;

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        _physics.SetLinearVelocity(uid, comp.OldLinearVelocity, false, body: physics);
        _physics.SetAngularVelocity(uid, comp.OldAngularVelocity, body: physics);
    }

    private void OnInit(Entity<FrozenComponent> ent, ref ComponentInit args)
    {
        var (uid, comp) = ent;

        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        comp.OldLinearVelocity = physics.LinearVelocity;
        comp.OldAngularVelocity = physics.AngularVelocity;

        _physics.SetLinearVelocity(uid, Vector2.Zero, false, body: physics);
        _physics.SetAngularVelocity(uid, 0f, body: physics);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<FrozenComponent>();

        while (query.MoveNext(out var uid, out var frozen))
        {
            frozen.Lifetime -= frameTime;

            if (frozen.Lifetime > 0)
                continue;

            RemCompDeferred<FrozenComponent>(uid);
        }
    }

    private void OnEntityExit(Entity<FreezeContactsComponent> ent, ref EndCollideEvent args)
    {
        if (_physics.GetContactingEntities(args.OtherEntity, args.OtherBody).Any(HasComp<FreezeContactsComponent>))
            return;

        RemCompDeferred<FrozenComponent>(args.OtherEntity);
    }

    private void OnEntityEnter(Entity<FreezeContactsComponent> ent, ref StartCollideEvent args)
    {
        var frozen = EnsureComp<FrozenComponent>(args.OtherEntity);

        if (!TryComp(ent, out TimedDespawnComponent? timedDespawn))
            return;

        frozen.Lifetime = timedDespawn.Lifetime;

        if (TryComp(args.OtherEntity, out TimedDespawnComponent? otherTimedDespawn))
            otherTimedDespawn.Lifetime += timedDespawn.Lifetime;

        if (!TryComp(args.OtherEntity, out ThrownItemComponent? thrownItem))
            return;

        if (thrownItem.LandTime != null)
            thrownItem.LandTime = thrownItem.LandTime.Value + TimeSpan.FromSeconds(timedDespawn.Lifetime);

        if (thrownItem.ThrownTime != null)
            thrownItem.ThrownTime = thrownItem.ThrownTime.Value + TimeSpan.FromSeconds(timedDespawn.Lifetime);
    }
}
