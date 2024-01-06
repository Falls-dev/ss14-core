using Content.Server.Actions;
using Content.Shared.Changeling;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorb = "ActionChangelingAbsorb";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransform = "ActionChangelingTransform";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingRegenerate = "ActionChangelingRegenerate";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingLesserForm = "ActionChangelingLesserForm";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentInit>(OnInit);

        InitializeAbilities();
    }


    private void OnInit(EntityUid uid, ChangelingComponent component, ComponentInit args)
    {
        CopyHumanoidData(uid, uid, component);
        _action.AddAction(uid, ref component.AbsorbAction, ChangelingAbsorb);
        _action.AddAction(uid, ref component.TransformAction, ChangelingTransform);
        _action.AddAction(uid, ref component.RegenerateAction, ChangelingRegenerate);
        _action.AddAction(uid, ref component.LesserFormAction, ChangelingLesserForm);
    }
}
