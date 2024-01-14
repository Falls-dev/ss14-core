using Content.Server.Objectives.Components;
using Content.Shared.Changeling;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

public sealed class ChangelingConditionsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Absorb DNA condition
        SubscribeLocalEvent<AbsorbDnaConditionComponent, ObjectiveAssignedEvent>(OnAbsorbDnaAssigned);
        SubscribeLocalEvent<AbsorbDnaConditionComponent, ObjectiveAfterAssignEvent>(OnAbsorbDnaAfterAssigned);
        SubscribeLocalEvent<AbsorbDnaConditionComponent, ObjectiveGetProgressEvent>(OnAbsorbDnaGetProgress);


    }

    private void OnAbsorbDnaAssigned(EntityUid uid, AbsorbDnaConditionComponent component, ref ObjectiveAssignedEvent args)
    {
        component.NeedToAbsorb = _random.Next(2, 6);
    }

    private void OnAbsorbDnaAfterAssigned(EntityUid uid, AbsorbDnaConditionComponent component, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString("objective-condition-absorb-dna", ("count", component.NeedToAbsorb));

        _metaData.SetEntityName(uid, title, args.Meta);
    }

    private void OnAbsorbDnaGetProgress(EntityUid uid, AbsorbDnaConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, component.NeedToAbsorb);
    }

    private float GetProgress(MindComponent mind, int requiredDna)
    {
        if (!TryComp<ChangelingComponent>(mind.CurrentEntity, out var changelingComponent))
            return 0f;

        var absorbed = changelingComponent.AbsorbedEntities.Count - 1; // Because first - it's the owner

        if (requiredDna == absorbed)
            return 1f;

        var progress = MathF.Min(absorbed/(float)requiredDna, 1f);

        return progress;
    }
}
