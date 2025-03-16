using Content.Shared._White.FlufSystems.merkka;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._White.FluffSystems.merkka;

public sealed class EarsSpawnSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EarsSpawnComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
        SubscribeLocalEvent<EarsSpawnComponent, GetItemActionsEvent>(GetSummonAction);
        SubscribeLocalEvent<EarsSpawnComponent, SummonActionEarsEvent>(OnSummon);
        SubscribeLocalEvent<EarsSpawnComponent, SummonActionCatEvent>(OnSummonCat);
        SubscribeLocalEvent<EarsSpawnComponent, ExaminedEvent>(OnExamined);
    }

    private const string Ears = "ClothingHeadHatCatEars";
    private const string Cat = "MobCatMurka";
    private const string UserNeededKey = "merkkaa";

    private void OnExamined(Entity<EarsSpawnComponent> entity, ref ExaminedEvent ev)
    {
        ev.PushMarkup($"Зарядов для ушей: {entity.Comp.CatEarsUses}\n" +
                      $"Зарядов для создания кота: {entity.Comp.СatSpawnUses}");
    }

    private void AddSummonVerb(Entity<EarsSpawnComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptSummon(entity, user);
            },
            Text = Loc.GetString("summon cat ears"),
            Priority = 2
        };

        AlternativeVerb verbCat = new()
        {
            Act = () =>
            {
                AttemptSummonCat(entity, user);
            },
            Text = Loc.GetString("summon cat"),
            Priority = 3
        };

        args.Verbs.Add(verb);
        args.Verbs.Add(verbCat);
    }

    private void OnSummon(Entity<EarsSpawnComponent> entity, ref SummonActionEarsEvent args)
    {
        AttemptSummon(entity, args.Performer);
    }

    private void OnSummonCat(Entity<EarsSpawnComponent> entity, ref SummonActionCatEvent args)
    {
        AttemptSummonCat(entity, args.Performer);
    }

    private void AttemptSummon(Entity<EarsSpawnComponent> entity, EntityUid user)
    {
        if (!_blocker.CanInteract(user, entity))
            return;

        if (TryComp<ActorComponent>(user, out var actorComponent))
        {
            var userKey = actorComponent.PlayerSession.Name;
            if (userKey != UserNeededKey)
            {
                _popupSystem.PopupEntity("Вы не являетесь потомком кошко-богини.", user, PopupType.Medium);
                return;
            }
        }

        if (entity.Comp.CatEarsUses == 0)
        {
            _popupSystem.PopupEntity("Больше нет зарядов!", user, PopupType.Medium);
            return;
        }

        SpawnEars(user, entity.Comp);
    }

    private void AttemptSummonCat(Entity<EarsSpawnComponent> entity, EntityUid user)
    {
        if (!_blocker.CanInteract(user, entity.Owner))
            return;

        if (TryComp<ActorComponent>(user, out var actorComponent))
        {
            var userKey = actorComponent.PlayerSession.Name;
            if (userKey != UserNeededKey)
            {
                _popupSystem.PopupEntity("Вы не являетесь потомком кошко-богини.", user, PopupType.Medium);
                return;
            }
        }

        if (entity.Comp.СatSpawnUses == 0)
        {
            _popupSystem.PopupEntity("Больше нет зарядов!", user, PopupType.Medium);
            return;
        }

        SpawnCat(user, entity);
    }

    private void SpawnEars(EntityUid player, EarsSpawnComponent comp)
    {
        var transform = CompOrNull<TransformComponent>(player)?.Coordinates;

        if (transform == null)
            return;

        var ears = _entityManager.SpawnEntity(Ears, transform.Value);
        _handsSystem.PickupOrDrop(player, ears);
        comp.CatEarsUses--;
    }

    private void SpawnCat(EntityUid player, EarsSpawnComponent comp)
    {
        var transform = CompOrNull<TransformComponent>(player)?.Coordinates;

        if (transform == null)
            return;

        _entityManager.SpawnEntity(Cat, transform.Value);
        comp.СatSpawnUses--;
    }

    private static void GetSummonAction(EntityUid uid, EarsSpawnComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.SummonActionEntityEars, component.SummonActionEars);
        args.AddAction(ref component.SummonActionEntityCat, component.SummonActionCat);
    }
}
