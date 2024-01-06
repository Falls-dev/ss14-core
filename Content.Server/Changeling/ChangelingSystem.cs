using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorb = "ActionChangelingAbsorb";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransform = "ActionChangelingTransform";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingRegenerate = "ActionChangelingRegenerate";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingLesserForm = "ActionChangelingLesserForm";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransformSting = "ActionTransformSting";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingBlindSting = "ActionBlindSting";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingMuteSting = "ActionMuteSting";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingHallucinationSting = "ActionHallucinationSting";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingCryoSting = "ActionCryoSting";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<AbsorbedComponent, ExaminedEvent>(OnExamine);

        InitializeAbilities();
    }


    private void OnInit(EntityUid uid, ChangelingComponent component, ComponentInit args)
    {
        CopyHumanoidData(uid, uid, component);
        _action.AddAction(uid, ref component.AbsorbAction, ChangelingAbsorb);
        _action.AddAction(uid, ref component.TransformAction, ChangelingTransform);
        _action.AddAction(uid, ref component.RegenerateAction, ChangelingRegenerate);
        _action.AddAction(uid, ref component.LesserFormAction, ChangelingLesserForm);
        _action.AddAction(uid, ref component.TransformStingAction, ChangelingTransformSting);
        _action.AddAction(uid, ref component.BlindStingAction, ChangelingBlindSting);
        _action.AddAction(uid, ref component.MuteStingAction, ChangelingMuteSting);
        _action.AddAction(uid, ref component.HallucinationStingAction, ChangelingHallucinationSting);
        _action.AddAction(uid, ref component.CryoStingAction, ChangelingCryoSting);
    }

    private void OnExamine(EntityUid uid, AbsorbedComponent component, ExaminedEvent args)
    {
        args.PushMarkup("[color=#A30000]His juices sucked up![/color]");
    }
}
