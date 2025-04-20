using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._White.Targeting;
using Content.Shared._White.Targeting.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly string[] _damageTypes = ["Slash", "Pierce", "Blunt"];

    private const double PartIntegrityJobTime = 0.005;
    private readonly JobQueue _PartIntegrityJobQueue = new(PartIntegrityJobTime);

    public sealed class PartIntegrityJob : Job<object>
    {
        private readonly SharedBodySystem _self;
        private readonly Entity<BodyPartComponent> _ent;
        public PartIntegrityJob(SharedBodySystem self, Entity<BodyPartComponent> ent, double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
            _self = self;
            _ent = ent;
        }

        public PartIntegrityJob(SharedBodySystem self, Entity<BodyPartComponent> ent, double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
        {
            _self = self;
            _ent = ent;
        }

        protected override Task<object?> Process()
        {
            _self.ProcessPartIntegrityTick(_ent);

            return Task.FromResult<object?>(null);
        }
    }

    private EntityQuery<TargetingComponent> _queryTargeting;
    private void InitializePartIntegrity()
    {
        _queryTargeting = GetEntityQuery<TargetingComponent>();
    }

    private void ProcessPartIntegrityTick(Entity<BodyPartComponent> entity)
    {
        if (entity.Comp is { Body: {} body, PartIntegrity: > 50 and < BodyPartComponent.MaxPartIntegrity }
            && _queryTargeting.HasComp(body)
            && !_mobState.IsDead(body))
        {
            var healing = entity.Comp.SelfHealingAmount;
            if (healing + entity.Comp.PartIntegrity > BodyPartComponent.MaxPartIntegrity)
                healing = entity.Comp.PartIntegrity - BodyPartComponent.MaxPartIntegrity;

            TryChangePartIntegrity(entity,
                healing,
                false,
                GetTargetBodyPart(entity),
                out _);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _PartIntegrityJobQueue.Process();

        if (!_timing.IsFirstTimePredicted)
            return;

        using var query = EntityQueryEnumerator<BodyPartComponent>();
        while (query.MoveNext(out var ent, out var part))
        {
            part.PartHealingTime += frameTime;

            if (part.PartHealingTimer >= part.PartHealingTime)
            {
                part.PartHealingTimer = 0;
                _PartIntegrityJobQueue.EnqueueJob(new PartIntegrityJob(this, (ent, part), PartIntegrityJobTime));
            }
        }
    }

    /// <summary>
    /// Propagates damage to the specified parts of the entity.
    /// </summary>
    private void ApplyPartDamage(Entity<BodyPartComponent> partEnt,
        DamageSpecifier damage,
        BodyPartType targetType,
        TargetingBodyParts targetPart,
        bool canSever,
        float partMultiplier)
    {
        if (partEnt.Comp.Body is null)
            return;

        foreach (var (damageType, damageValue) in damage.DamageDict)
        {
            if (damageValue.Float() == 0
                || TryEvadeDamage(partEnt.Comp.Body.Value, GetEvadeChance(targetType)))
                continue;

            var modifier = GetDamageModifier(damageType);
            var partModifier = GetPartDamageModifier(targetType);
            var partIntegrityDamage = damageValue.Float() * modifier * partModifier * partMultiplier;

            TryChangePartIntegrity(partEnt,
                partIntegrityDamage,
                canSever && _damageTypes.Contains(damageType),
                targetPart,
                out var severed);

            if (severed)
                break;
        }
    }

    public void TryChangePartIntegrity(Entity<BodyPartComponent> partEnt,
        float partIntegrity,
        bool canSever,
        TargetingBodyParts? targetPart,
        out bool severed)
    {
        severed = false;
        if (!_timing.IsFirstTimePredicted || !_queryTargeting.HasComp(partEnt.Comp.Body))
            return;

        var partIdSlot = GetParentPartAndSlotOrNull(partEnt)?.Slot;
        var originalPartIntegrity = partEnt.Comp.PartIntegrity;
        partEnt.Comp.PartIntegrity = Math.Min(BodyPartComponent.MaxPartIntegrity, partEnt.Comp.PartIntegrity - partIntegrity);
        if (canSever
            && !partEnt.Comp.Enabled
            && partEnt.Comp.PartIntegrity <= 0
            && partIdSlot is not null)
            severed = true;

        switch (partEnt.Comp.Enabled)
        {
            case true
            when partEnt.Comp.PartIntegrity <= 15.0f:
            {
                var ev = new TargetingBodyPartEnableChangedEvent(false);
                RaiseLocalEvent(partEnt, ref ev);
                break;
            }
            case false
            when partEnt.Comp.PartIntegrity >= 80.0f:
            {
                var ev = new TargetingBodyPartEnableChangedEvent(true);
                RaiseLocalEvent(partEnt, ref ev);
                break;
            }
        }

        if (partEnt.Comp.PartIntegrity != originalPartIntegrity
            && _queryTargeting.TryComp(partEnt.Comp.Body, out var targeting)
            && HasComp<MobStateComponent>(partEnt.Comp.Body))
        {
            var newPartIntegrity = GetPartIntegrityThreshold(partEnt.Comp.PartIntegrity, severed, partEnt.Comp.Enabled);

            if (targetPart is not null && targeting.TargetIntegrities[targetPart.Value] != TargetIntegrity.Dead)
            {
                targeting.TargetIntegrities[targetPart.Value] = newPartIntegrity;
                Dirty(partEnt.Comp.Body.Value, targeting);
            }

            if (_net.IsServer)
                RaiseNetworkEvent(new TargetingIntegrityChangeEvent(GetNetEntity(partEnt.Comp.Body.Value)), partEnt.Comp.Body.Value);
        }

        if (severed && partIdSlot is not null)
            DropPart(partEnt);

        Dirty(partEnt, partEnt.Comp);
    }

    public Dictionary<TargetingBodyParts, TargetIntegrity> GetBodyPartStatus(EntityUid entityUid)
    {
        var result = new Dictionary<TargetingBodyParts, TargetIntegrity>();

        if (!TryComp<BodyComponent>(entityUid, out var body))
            return result;

        foreach (TargetingBodyParts part in Enum.GetValues(typeof(TargetingBodyParts)))
        {
            result[part] = TargetIntegrity.Severed;
        }

        foreach (var partComponent in GetBodyChildren(entityUid, body))
        {
            var targetBodyPart = GetTargetBodyPart(partComponent.Component.PartType, partComponent.Component.Symmetry);

            if (targetBodyPart != null)
            {
                result[targetBodyPart.Value] = GetPartIntegrityThreshold(partComponent.Component.PartIntegrity, false, partComponent.Component.Enabled);
            }
        }

        return result;
    }

    public TargetingBodyParts? GetTargetBodyPart(Entity<BodyPartComponent> part)
    {
        return GetTargetBodyPart(part.Comp.PartType, part.Comp.Symmetry);
    }

    public TargetingBodyParts? GetTargetBodyPart(BodyPartComponent part)
    {
        return GetTargetBodyPart(part.PartType, part.Symmetry);
    }

    public TargetingBodyParts? GetTargetBodyPart(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return (type, symmetry) switch
        {
            (BodyPartType.Head, _) => TargetingBodyParts.Head,
            (BodyPartType.Torso, _) => TargetingBodyParts.Chest,
            (BodyPartType.Arm, BodyPartSymmetry.Left) => TargetingBodyParts.LeftArm,
            (BodyPartType.Arm, BodyPartSymmetry.Right) => TargetingBodyParts.RightArm,
            (BodyPartType.Leg, BodyPartSymmetry.Left) => TargetingBodyParts.LeftLeg,
            (BodyPartType.Leg, BodyPartSymmetry.Right) => TargetingBodyParts.RightLeg,
            _ => null
        };
    }

    public (BodyPartType Type, BodyPartSymmetry Symmetry) ConvertTargetBodyPart(TargetingBodyParts targetPart)
    {
        return targetPart switch
        {
            TargetingBodyParts.Head => (BodyPartType.Head, BodyPartSymmetry.None),
            TargetingBodyParts.Chest => (BodyPartType.Torso, BodyPartSymmetry.None),
            TargetingBodyParts.Stomach =>  (BodyPartType.Torso, BodyPartSymmetry.None),
            TargetingBodyParts.LeftArm => (BodyPartType.Arm, BodyPartSymmetry.Left),
            TargetingBodyParts.LeftHand => (BodyPartType.Hand, BodyPartSymmetry.Left),
            TargetingBodyParts.RightArm => (BodyPartType.Arm, BodyPartSymmetry.Right),
            TargetingBodyParts.RightHand => (BodyPartType.Hand, BodyPartSymmetry.Right),
            TargetingBodyParts.LeftLeg => (BodyPartType.Leg, BodyPartSymmetry.Left),
            TargetingBodyParts.LeftFoot => (BodyPartType.Foot, BodyPartSymmetry.Left),
            TargetingBodyParts.RightLeg => (BodyPartType.Leg, BodyPartSymmetry.Right),
            TargetingBodyParts.RightFoot => (BodyPartType.Foot, BodyPartSymmetry.Right),
            _ => (BodyPartType.Torso, BodyPartSymmetry.None)
        };

    }

    public float GetDamageModifier(string damageType)
    {
        return damageType switch
        {
            "Blunt" => 0.8f,
            "Slash" => 1.2f,
            "Pierce" => 0.5f,
            "Heat" => 1.0f,
            "Cold" => 1.0f,
            "Shock" => 0.8f,
            "Poison" => 0.8f,
            "Radiation" => 0.8f,
            "Cellular" => 0.8f,
            _ => 0.5f
        };
    }

    public float GetPartDamageModifier(BodyPartType partType)
    {
        return partType switch
        {
            BodyPartType.Head => 0.5f,
            BodyPartType.Torso => 1.0f,
            BodyPartType.Arm => 0.7f,
            BodyPartType.Leg => 0.7f,
            _ => 0.5f
        };
    }

    public TargetIntegrity GetPartIntegrityThreshold(float PartIntegrity, bool severed, bool enabled)
    {
        if (severed)
            return TargetIntegrity.Severed;

        if (!enabled)
            return TargetIntegrity.Disabled;

        return PartIntegrity switch
        {
            <= 10.0f => TargetIntegrity.CriticallyWounded,
            <= 25.0f => TargetIntegrity.HeavilyWounded,
            <= 40.0f => TargetIntegrity.ModeratelyWounded,
            <= 60.0f => TargetIntegrity.SomewhatWounded,
            <= 80.0f => TargetIntegrity.LightlyWounded,
            _ => TargetIntegrity.Healthy
        };
    }

    public float GetEvadeChance(BodyPartType partType)
    {
        return partType switch
        {
            BodyPartType.Head => 0.70f,
            BodyPartType.Arm => 0.20f,
            BodyPartType.Leg => 0.20f,
            BodyPartType.Torso => 0f,
            _ => 0f
        };
    }

    public bool CanEvadeDamage(EntityUid uid)
    {
        return TryComp<MobStateComponent>(uid, out var mobState)
               && TryComp<StandingStateComponent>(uid, out var standingState)
               && !_mobState.IsCritical(uid, mobState)
               && !_mobState.IsDead(uid, mobState)
               && standingState.CurrentState != StandingState.Lying;
    }

    public bool TryEvadeDamage(EntityUid uid, float evadeChance)
    {
        if (!CanEvadeDamage(uid))
            return false;

        return _random.NextFloat() < evadeChance;
    }
}
