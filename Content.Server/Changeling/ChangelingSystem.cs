using System.Linq;
using Content.Server.Actions;
using Content.Server.Database;
using Content.Server.DoAfter;
using Content.Server.Forensics;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling;

public sealed class ChangelingSystem: EntitySystem
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


    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorb = "ActionChangelingAbsorb";
    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingTransform = "ActionChangelingTransform";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaActionEvent>(OnAbsorb);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaDoAfterEvent>(OnAbsorbDoAfter);

        SubscribeLocalEvent<ChangelingComponent, TransformActionEvent>(OnTransform);
        SubscribeLocalEvent<ChangelingComponent, TransformDoAfterEvent>(OnTransformDoAfter);
    }

    private void OnTransform(EntityUid uid, ChangelingComponent component, TransformActionEvent args)
    {
        if (component.AbsorbedEntities.Count <= 1)
        {
            _popup.PopupEntity("You don't have any persons to transform!", uid);
            return;
        }

        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, uid, component.TransformDelay, new TransformDoAfterEvent(), uid,
                uid, uid)
            {
                BreakOnUserMove = true
            });
    }

    private void TryTransform(EntityUid uid, ChangelingComponent component)
    {
        var person = component.AbsorbedEntities.Last();

        if(!TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        ClonePerson(uid,person.Value.AppearanceComponent, appearance);


        if(!TryComp<MetaDataComponent>(uid, out var meta))
            return;

        _metaData.SetEntityName(uid, person.Value.MetaDataComponent!.EntityName, meta);
        _metaData.SetEntityDescription(uid, person.Value.MetaDataComponent!.EntityDescription, meta);

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

    private void OnTransformDoAfter(EntityUid uid, ChangelingComponent component, TransformDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        TryTransform(uid, component);

        args.Handled = true;
    }

    private void OnInit(EntityUid uid, ChangelingComponent component, ComponentInit args)
    {
        CopyHumanoidData(uid, uid, component);
        _action.AddAction(uid, ref component.AbsorbAction, ChangelingAbsorb);
        _action.AddAction(uid, ref component.TransformAction, ChangelingTransform);
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
            Name = proto.Name,
            EntityUid = target
        });

        Dirty(uid, component);
    }

    private void OnAbsorb(EntityUid uid, ChangelingComponent component, AbsorbDnaActionEvent args)
    {
        TryAbsorb(uid, args.Performer, args.Target, component);
    }

    private void TryAbsorb(EntityUid uid, EntityUid performer, EntityUid target, ChangelingComponent component)
    {
        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popup.PopupEntity("You can't absorb not humans!", performer);
            return;
        }

        if (HasComp<AbsorbedComponent>(target))
        {
            _popup.PopupEntity("This person already absorbed!", performer);
            return;
        }

        if (!_stateSystem.IsDown(target))
        {
            _popup.PopupEntity("Target must be down!", performer);
            return;
        }

        if (!TryComp<SharedPullableComponent>(target, out var pulled))
        {
            _popup.PopupEntity("You must pull target!", performer);
            return;
        }

        if (!pulled.BeingPulled)
        {
            _popup.PopupEntity("You must pull target!", performer);
            return;
        }


        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, performer, component.AbsorbDnaDelay, new AbsorbDnaDoAfterEvent(), uid,
                target, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true
            });
    }
}
