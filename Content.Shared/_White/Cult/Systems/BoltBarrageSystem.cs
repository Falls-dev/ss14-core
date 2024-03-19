using System.Linq;
using Content.Shared._White.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._White.Cult.Systems;

public sealed class BoltBarrageSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BoltBarrageComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<BoltBarrageComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<BoltBarrageComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<BoltBarrageComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<BoltBarrageComponent, OnEmptyGunShotEvent>(OnEmptyShot);
    }

    private void OnEmptyShot(Entity<BoltBarrageComponent> ent, ref OnEmptyGunShotEvent args)
    {
        if (_net.IsServer)
            QueueDel(ent);
    }

    private void OnInsert(Entity<BoltBarrageComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!HasComp<BodyComponent>(args.Container.Owner) && _net.IsServer)
            QueueDel(ent);
    }

    private void OnDrop(Entity<BoltBarrageComponent> ent, ref DroppedEvent args)
    {
        if (_net.IsServer)
            QueueDel(ent);
    }

    private void OnGunShot(Entity<BoltBarrageComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp(args.User, out HandsComponent? hands))
            return;

        foreach (var hand in _hands.EnumerateHands(args.User, hands))
        {
            if (!hand.IsEmpty)
                continue;

            _hands.SetActiveHand(args.User, hand, hands);
            _hands.TryPickup(args.User, ent, hand, false, false, hands);
            return;
        }
    }

    private void OnShootAttempt(Entity<BoltBarrageComponent> ent, ref AttemptShootEvent args)
    {
        if (!HasComp<CultistComponent>(args.User))
        {
            args.Cancelled = true;
            return;
        }

        if (_hands.EnumerateHands(args.User).Any(hand => hand.IsEmpty))
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString("bolt-barrage-component-no-empty-hand");
    }
}
