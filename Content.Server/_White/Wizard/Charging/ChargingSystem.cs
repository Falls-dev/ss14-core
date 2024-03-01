using Content.Shared._White.Wizard;
using Content.Shared._White.Wizard.Charging;
using Content.Shared.Follower;

namespace Content.Server._White.Wizard.Charging;

public sealed class ChargingSystem : SharedChargingSystem
{
    [Dependency] private readonly FollowerSystem _followerSystem = default!;

    private Dictionary<EntityUid, List<EntityUid>> _charges = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AddWizardChargeEvent>(Add);
        SubscribeNetworkEvent<RemoveWizardChargeEvent>(Remove);
    }

    private void Add(AddWizardChargeEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            AddCharge(args.SenderSession.AttachedEntity.Value);
    }

    private void Remove(RemoveWizardChargeEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            RemoveAllCharges(args.SenderSession.AttachedEntity.Value);
    }

    public void AddCharge(EntityUid uid)
    {
        var itemEnt = Spawn("MagicFollowerEntity", Transform(uid).Coordinates);
        _followerSystem.StartFollowingEntity(itemEnt, uid);

        if (!_charges.ContainsKey(uid))
        {
            _charges[uid] = new List<EntityUid>();
        }

        _charges[uid].Add(itemEnt);
    }


    public void RemoveCharge(EntityUid uid)
    {
        if (!_charges.ContainsKey(uid))
            return;

        foreach (var followerEnt in _charges[uid])
        {
            Del(followerEnt);
        }

        _charges.Remove(uid);
    }

    public void RemoveAllCharges(EntityUid uid)
    {
        if (!_charges.ContainsKey(uid))
            return;

        foreach (var followerEnt in _charges[uid])
        {
            Del(followerEnt);
        }

        _charges.Remove(uid);
    }
}
