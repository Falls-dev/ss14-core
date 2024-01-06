using System.Linq;
using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Forensics;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Popups;
using Content.Shared.Changeling;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Miracle.UI;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorb = "ActionChangelingAbsorb";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransform = "ActionChangelingTransform";

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
    }
}
