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

namespace Content.Server.Changeling;

public sealed class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StandingStateSystem _stateSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorb = "ActionChangelingAbsorb";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransform = "ActionChangelingTransform";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaActionEvent>(OnAbsorb);
        SubscribeLocalEvent<ChangelingComponent, TransformActionEvent>(OnTransform);

        SubscribeLocalEvent<ChangelingComponent, TransformDoAfterEvent>(OnTransformDoAfter);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaDoAfterEvent>(OnAbsorbDoAfter);

        SubscribeLocalEvent<ChangelingComponent, ListViewItemSelectedMessage>(OnTransformUiMessage);
    }

    #region Handlers

    private void OnInit(EntityUid uid, ChangelingComponent component, ComponentInit args)
    {
        CopyHumanoidData(uid, uid, component);
        _action.AddAction(uid, ref component.AbsorbAction, ChangelingAbsorb);
        _action.AddAction(uid, ref component.TransformAction, ChangelingTransform);
    }

    private void OnAbsorb(EntityUid uid, ChangelingComponent component, AbsorbDnaActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity("You can't absorb not humans!", args.Performer);
            return;
        }

        if (HasComp<AbsorbedComponent>(args.Target))
        {
            _popup.PopupEntity("This person already absorbed!", args.Performer);
            return;
        }

        if (!_stateSystem.IsDown(args.Target))
        {
            _popup.PopupEntity("Target must be down!", args.Performer);
            return;
        }

        if (!TryComp<SharedPullableComponent>(args.Target, out var pulled))
        {
            _popup.PopupEntity("You must pull target!", args.Performer);
            return;
        }

        if (!pulled.BeingPulled)
        {
            _popup.PopupEntity("You must pull target!", args.Performer);
            return;
        }


        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, args.Performer, component.AbsorbDnaDelay, new AbsorbDnaDoAfterEvent(), uid,
                args.Target, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true
            });
    }

    private void OnTransform(EntityUid uid, ChangelingComponent component, TransformActionEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (component.AbsorbedEntities.Count <= 1)
        {
            _popup.PopupEntity("You don't have any persons to transform!", uid);
            return;
        }

        if (!_ui.TryGetUi(uid, ListViewSelectorUiKey.Key, out var bui))
            return;

        var state = component.AbsorbedEntities.ToDictionary(humanoidData
            => humanoidData.Key, humanoidData
            => humanoidData.Value.Name);

        _ui.SetUiState(bui, new ListViewBuiState(state));
        _ui.OpenUi(bui, actorComponent.PlayerSession);
    }

    private void OnTransformUiMessage(EntityUid uid, ChangelingComponent component, ListViewItemSelectedMessage args)
    {
        var selectedDna = args.SelectedItem;
        var user = GetEntity(args.Entity);

        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, user, component.TransformDelay, new TransformDoAfterEvent{SelectedDna = selectedDna}, user,
                user, user)
            {
                BreakOnUserMove = true
            });

        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (!_ui.TryGetUi(user, ListViewSelectorUiKey.Key, out var bui))
            return;

        _ui.CloseUi(bui, actorComponent.PlayerSession);
    }

    #endregion

    #region DoAfters

    private void OnTransformDoAfter(EntityUid uid, ChangelingComponent component, TransformDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        TryTransform(args.User, args.SelectedDna, component);

        args.Handled = true;
    }

    private void OnAbsorbDoAfter(EntityUid uid, ChangelingComponent component, AbsorbDnaDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
        {
            return;
        }

        CopyHumanoidData(uid, (EntityUid) args.Target, component);

        if(!_mobThresholdSystem.TryGetThresholdForState((EntityUid) args.Target, MobState.Dead, out var damage))
            return;

        DamageSpecifier dmg = new();
        dmg.DamageDict.Add("Bloodloss", damage.Value); //todo change damage type
        _damage.TryChangeDamage(args.Target, dmg, true, origin: uid);

        EnsureComp<AbsorbedComponent>((EntityUid) args.Target);

        args.Handled = true;
    }

    #endregion

    #region Helpers

    private void CopyHumanoidData(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if(!TryComp<MetaDataComponent>(target, out var targetMeta))
            return;
        if(!TryComp<HumanoidAppearanceComponent>(target, out var targetAppearance))
            return;
        if(!TryComp<DnaComponent>(target, out var targetDna))
            return;
        if (!TryPrototype(target, out var proto, targetMeta))
            return;

        if(component.AbsorbedEntities.ContainsKey(proto.Name))
            return;

        component.AbsorbedEntities.Add(targetDna.DNA, new HumanoidData
        {
            EntityPrototype = proto,
            MetaDataComponent = targetMeta,
            AppearanceComponent = targetAppearance,
            Name = targetMeta.EntityName,
            EntityUid = target,
            Dna = targetDna.DNA
        });

        Dirty(uid, component);
    }

    private void TryTransform(EntityUid uid, string dna, ChangelingComponent component)
    {
        if (!component.AbsorbedEntities.TryGetValue(dna, out var person))
        {
            return;
        }

        if(!TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        ClonePerson(uid,person.AppearanceComponent, appearance);


        if(!TryComp<MetaDataComponent>(uid, out var meta))
            return;

        _metaData.SetEntityName(uid, person.MetaDataComponent!.EntityName, meta);
        _metaData.SetEntityDescription(uid, person.MetaDataComponent!.EntityDescription, meta);

        _identity.QueueIdentityUpdate(uid);
    }

    private void ClonePerson(EntityUid target, HumanoidAppearanceComponent sourceHumanoid,
        HumanoidAppearanceComponent targetHumanoid)
    {
        targetHumanoid.Species = sourceHumanoid.Species;
        targetHumanoid.SkinColor = sourceHumanoid.SkinColor;
        targetHumanoid.EyeColor = sourceHumanoid.EyeColor;
        targetHumanoid.Age = sourceHumanoid.Age;
        _humanoidAppearance.SetSex(target, sourceHumanoid.Sex, false, targetHumanoid);
        targetHumanoid.CustomBaseLayers = new Dictionary<HumanoidVisualLayers,
            CustomBaseLayerInfo>(sourceHumanoid.CustomBaseLayers);
        targetHumanoid.MarkingSet = new MarkingSet(sourceHumanoid.MarkingSet);

        targetHumanoid.Gender = sourceHumanoid.Gender;
        if (TryComp<GrammarComponent>(target, out var grammar))
        {
            grammar.Gender = sourceHumanoid.Gender;
        }

        Dirty(target, targetHumanoid);
    }

    #endregion
}
