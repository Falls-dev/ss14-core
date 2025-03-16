using Content.Server.Ghost.Roles.Components;
using Content.Shared._White.FlufSystems.PetSummonSystem;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._White.FluffSystems.PetSummonSystem;

public sealed class PetSummonSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly IReadOnlyDictionary<string, string> _mobMap = new Dictionary<string, string>()
    {
        { "Wanderer_", "KommandantPetSpider" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PetSummonComponent, GetItemActionsEvent>(GetSummonAction);
        SubscribeLocalEvent<PetSummonComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PetSummonComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
        SubscribeLocalEvent<PetSummonComponent, PetSummonActionEvent>(OnSummon);
        SubscribeLocalEvent<PetSummonComponent, PetGhostSummonActionEvent>(OnGhostSummon);
    }

    private void OnGhostSummon(Entity<PetSummonComponent> entity, ref PetGhostSummonActionEvent args)
    {
        AttemptSummon(entity, args.Performer, true);
    }

    private void OnSummon(Entity<PetSummonComponent> entity, ref PetSummonActionEvent args)
    {
        AttemptSummon(entity, args.Performer, false);
    }

    private void AddSummonVerb(Entity<PetSummonComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptSummon(entity, user, false);
            },
            Text = "Призвать питомца",
            Priority = 2
        };

        AlternativeVerb ghostVerb = new()
        {
            Act = () =>
            {
                AttemptSummon(entity, user, true);
            },
            Text = "Призвать питомца-призрак",
            Priority = 2
        };

        args.Verbs.Add(verb);
        args.Verbs.Add(ghostVerb);
    }

    private void OnExamine(EntityUid uid, PetSummonComponent component, ExaminedEvent args)
    {
        args.PushMarkup($"Осталось призывов: {component.UsesLeft}");
    }

    private void AttemptSummon(Entity<PetSummonComponent> entity, EntityUid user, bool ghostRole)
    {
        if (!_blocker.CanInteract(user, entity))
            return;

        string? mobProto = null;
        if (TryComp<ActorComponent>(user, out var actorComponent))
        {
            var userKey = actorComponent.PlayerSession.Name;

            if (!_mobMap.TryGetValue(userKey, out var proto))
            {
                _popupSystem.PopupEntity("Вы не достойны", user, PopupType.Medium);
                return;
            }

            mobProto = proto;
        }

        if (entity.Comp.UsesLeft == 0)
        {
            _popupSystem.PopupEntity("Больше нет зарядов!", user, PopupType.Medium);
            return;
        }

        if (entity.Comp.SummonedEntity != null)
        {
            if (!TryComp<MobStateComponent>(entity.Comp.SummonedEntity, out var mobState))
            {
                entity.Comp.SummonedEntity = null;
            }
            else
            {
                if (mobState.CurrentState is MobState.Dead or MobState.Invalid)
                    entity.Comp.SummonedEntity = null;
                else
                {
                    _popupSystem.PopupEntity("Ваш питомец уже призван", user, PopupType.Medium);
                    return;
                }
            }

        }

        if (mobProto != null)
            SummonPet(user, entity, mobProto, ghostRole);
    }

    private void SummonPet(EntityUid user, PetSummonComponent component, string mobProto, bool ghostRole)
    {
        var transform = CompOrNull<TransformComponent>(user)?.Coordinates;

        if (transform == null)
            return;

        var entity = _entityManager.SpawnEntity(mobProto, transform.Value);
        component.UsesLeft--;
        component.SummonedEntity = entity;

        if (ghostRole)
            SetupGhostRole(entity, user);
        else
            RemComp<GhostRoleComponent>(entity);
    }

    private void SetupGhostRole(EntityUid entity, EntityUid user)
    {
        EnsureComp<GhostTakeoverAvailableComponent>(entity);

        if (!TryComp<GhostRoleComponent>(entity, out var ghostRole))
            return;

        var meta = MetaData(user);
        ghostRole.RoleName = $"Питомец {meta.EntityName}";
        ghostRole.RoleDescription = $"Следуйте за хозяином - {meta.EntityName} и выполняйте его приказы";
        ghostRole.RoleRules = $"Вы должны до самого конца следовать за своим хозяином - {meta.EntityName} и послушно выполнять его приказы, иначе можете быть уничтожены.";
    }

    private void GetSummonAction(EntityUid uid, PetSummonComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.PetSummonActionEntity, component.PetSummonAction);
        args.AddAction(ref component.PetGhostSummonActionEntity, component.PetGhostSummonAction);
    }
}
