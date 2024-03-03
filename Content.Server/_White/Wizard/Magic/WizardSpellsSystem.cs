using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Lightning;
using Content.Server.Magic;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._White.Wizard;
using Content.Shared.Actions;
using Content.Shared.Magic;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server._White.Wizard.Magic;

public sealed class WizardSpellsSystem : EntitySystem
{
    #region Dependencies

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly MagicSystem _magicSystem = default!;
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;

    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForceSpellEvent>(OnForceSpell);
        SubscribeLocalEvent<ArcSpellEvent>(OnArcSpell);
    }

    #region Force

    private void OnForceSpell(ForceSpellEvent msg)
    {
        if (msg.Handled)
            return;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                ForceSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                ForceSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                ForceSpellAlt(msg);
                break;
        }

        msg.Handled = true;
        Speak(msg);
    }

    private void ForceSpellDefault(ForceSpellEvent msg)
    {
        Spawn("AdminInstantEffectMinusGravityWell", msg.Target);
    }

    private void ForceSpellCharge(ForceSpellEvent msg)
    {
        _gravityWell.GravPulse(msg.Performer, 15, 0, -80 * msg.ChargeLevel, -2 * msg.ChargeLevel);
    }

    private void ForceSpellAlt(ForceSpellEvent msg)
    {
        _gravityWell.GravPulse(msg.Target, 10, 0, 200, 10);
    }

    #endregion

    #region Arc

    private void OnArcSpell(ArcSpellEvent msg)
    {
        if (msg.Handled)
            return;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                ArcSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                ArcSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                ArcSpellAlt(msg);
                break;
        }

        msg.Handled = true;
        Speak(msg);
    }

    private void ArcSpellDefault(ArcSpellEvent args)
    {
        const int possibleEntitiesCount = 2;

        var entitiesInRange = _lookup.GetEntitiesInRange(args.Target, 1);
        var entitiesToHit = entitiesInRange.Where(HasComp<MobStateComponent>).Take(possibleEntitiesCount);

        foreach (var entity in entitiesToHit)
        {
            _lightning.ShootLightning(args.Performer, entity);
        }
    }

    private void ArcSpellCharge(ArcSpellEvent args)
    {
        _lightning.ShootRandomLightnings(args.Performer, 2 * args.ChargeLevel, args.ChargeLevel * 2, arcDepth: 2);
    }

    private void ArcSpellAlt(ArcSpellEvent args)
    {
        var xform = Transform(args.Performer);

        foreach (var pos in _magicSystem.GetSpawnPositions(xform, args.Pos))
        {
            var mapPos = _transformSystem.ToMapCoordinates(pos);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out var grid)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var userVelocity = Vector2.Zero;

            if (grid != null && TryComp(gridUid, out PhysicsComponent? physics))
                userVelocity = physics.LinearVelocity;

            var ent = Spawn(args.Prototype, spawnCoords);
            var direction = args.Target.ToMapPos(EntityManager, _transformSystem) - spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, args.Performer, args.Performer);
        }
    }

    #endregion

    #region Helpers

    private void Speak(BaseActionEvent args)
    {
        if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);
    }

    #endregion
}
