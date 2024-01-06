using System.Linq;
using Content.Server.Administration.Systems;
using Content.Server.DoAfter;
using Content.Server.Forensics;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Muting;
using Content.Shared.Changeling;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Miracle.UI;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StandingStateSystem _stateSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly BlindableSystem _blindingSystem = default!;


    private void InitializeAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaActionEvent>(OnAbsorb);
        SubscribeLocalEvent<ChangelingComponent, TransformActionEvent>(OnTransform);
        SubscribeLocalEvent<ChangelingComponent, RegenerateActionEvent>(OnRegenerate);
        SubscribeLocalEvent<ChangelingComponent, LesserFormActionEvent>(OnLesserForm);

        SubscribeLocalEvent<ChangelingComponent, TransformStingActionEvent>(OnTransformSting);
        SubscribeLocalEvent<ChangelingComponent, TransformStingItemSelectedMessage>(OnTransformStingMessage);
        SubscribeLocalEvent<ChangelingComponent, BlindStingActionEvent>(OnBlindSting);
        SubscribeLocalEvent<ChangelingComponent, MuteStingActionEvent>(OnMuteSting);

        SubscribeLocalEvent<ChangelingComponent, TransformDoAfterEvent>(OnTransformDoAfter);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaDoAfterEvent>(OnAbsorbDoAfter);
        SubscribeLocalEvent<ChangelingComponent, RegenerateDoAfterEvent>(OnRegenerateDoAfter);
        SubscribeLocalEvent<ChangelingComponent, LesserFormDoAfterEvent>(OnLesserFormDoAfter);

        SubscribeLocalEvent<ChangelingComponent, ListViewItemSelectedMessage>(OnTransformUiMessage);
    }

    #region Handlers

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

        if (!TryComp<DnaComponent>(args.Target, out var dnaComponent))
        {
           _popup.PopupEntity("Unknown creature!", uid);
            return;
        }

        if (component.AbsorbedEntities.ContainsKey(dnaComponent.DNA))
        {
            _popup.PopupEntity("This DNA already absorbed!", uid);
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


        Dictionary<string, string> state;

        if (TryComp<DnaComponent>(uid, out var dnaComponent))
        {
            state = component.AbsorbedEntities.Where(key => key.Key != dnaComponent.DNA).ToDictionary(humanoidData
                => humanoidData.Key, humanoidData
                => humanoidData.Value.Name);
        }
        else
        {
            state = component.AbsorbedEntities.ToDictionary(humanoidData
                => humanoidData.Key, humanoidData
                => humanoidData.Value.Name);
        }

        _ui.SetUiState(bui, new ListViewBuiState(state));
        _ui.OpenUi(bui, actorComponent.PlayerSession);
    }

    private void OnTransformUiMessage(EntityUid uid, ChangelingComponent component, ListViewItemSelectedMessage args)
    {
        var selectedDna = args.SelectedItem;
        var user = GetEntity(args.Entity);

        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, user, component.TransformDelay,
                new TransformDoAfterEvent { SelectedDna = selectedDna }, user,
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

    private void OnRegenerate(EntityUid uid, ChangelingComponent component, RegenerateActionEvent args)
    {
        if(!TryComp<DamageableComponent>(uid, out var damageableComponent))
            return;

        if (damageableComponent.TotalDamage >= 0 && !_mobStateSystem.IsDead(uid))
        {
            KillUser(uid, "Cellular") ;
        }

        _popup.PopupEntity("We beginning our regeneration.", uid);

        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, args.Performer, component.RegenerateDelay,
                new RegenerateDoAfterEvent(), args.Performer,
                args.Performer, args.Performer)
            {
                RequireCanInteract = false
            });

        component.IsRegenerating = true;
    }

    private void OnLesserForm(EntityUid uid, ChangelingComponent component, LesserFormActionEvent args)
    {
        if (_mobStateSystem.IsDead(uid) || component.IsRegenerating)
        {
            _popup.PopupEntity("We can do this right now!", uid);
            return;
        }


        if(component.IsLesserForm)
        {
            _popup.PopupEntity("We're already in the lesser form!", uid);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, component.LesserFormDelay,
            new LesserFormDoAfterEvent(), args.Performer, args.Performer)
        {
            BreakOnUserMove = true
        });
    }

    private void OnTransformSting(EntityUid uid, ChangelingComponent component, TransformStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity("We can't transform that!", args.Performer);
            return;
        }

        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (component.AbsorbedEntities.Count < 1)
        {
            _popup.PopupEntity("You don't have any persons to transform!", uid);
            return;
        }

        if (!_ui.TryGetUi(uid, TransformStingSelectorUiKey.Key, out var bui))
            return;

        var target = GetNetEntity(args.Target);

        var state = component.AbsorbedEntities.ToDictionary(humanoidData
            => humanoidData.Key, humanoidData
            => humanoidData.Value.Name);

        _ui.SetUiState(bui, new TransformStingBuiState(state, target));
        _ui.OpenUi(bui, actorComponent.PlayerSession);
    }

    private void OnTransformStingMessage(EntityUid uid, ChangelingComponent component, TransformStingItemSelectedMessage args)
    {
        var selectedDna = args.SelectedItem;
        var humanData = component.AbsorbedEntities[selectedDna];
        var target = GetEntity(args.Target);
        var user = GetEntity(args.Entity);

        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (!_ui.TryGetUi(user, TransformStingSelectorUiKey.Key, out var bui))
            return;

        TransformPerson(target, humanData);

        _ui.CloseUi(bui, actorComponent.PlayerSession);

        _action.StartUseDelay(component.TransformStingAction);
    }

    private void OnBlindSting(EntityUid uid, ChangelingComponent component, BlindStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target) ||
            !TryComp<BlindableComponent>(args.Target, out var blindable))
        {
            _popup.PopupEntity("We cannot sting that!", uid);
            return;
        }

        _blindingSystem.AdjustEyeDamage((args.Target, blindable), 1);
        var statusTimeSpan = TimeSpan.FromSeconds(25 * MathF.Sqrt(blindable.EyeDamage));
        _statusEffectsSystem.TryAddStatusEffect(args.Target, TemporaryBlindnessSystem.BlindingStatusEffect,
            statusTimeSpan, false, TemporaryBlindnessSystem.BlindingStatusEffect);

        args.Handled = true;
    }

    private void OnMuteSting(EntityUid uid, ChangelingComponent component, MuteStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity("We cannot sting that!", uid);
            return;
        }

        var statusTimeSpan = TimeSpan.FromSeconds(30);
        _statusEffectsSystem.TryAddStatusEffect(args.Target, "Muted",
            statusTimeSpan, false, "Muted");

        args.Handled = true;
    }

    #endregion

    #region DoAfters

    private void OnTransformDoAfter(EntityUid uid, ChangelingComponent component, TransformDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        TryTransformChangeling(args.User, args.SelectedDna, component);

        _action.StartUseDelay(component.TransformAction);

        args.Handled = true;
    }

    private void OnAbsorbDoAfter(EntityUid uid, ChangelingComponent component, AbsorbDnaDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
        {
            return;
        }

        CopyHumanoidData(uid, args.Target.Value, component);

        KillUser(args.Target.Value, "Cellular");

        EnsureComp<AbsorbedComponent>(args.Target.Value);

        _action.StartUseDelay(component.AbsorbAction);

        args.Handled = true;
    }

    private void OnRegenerateDoAfter(EntityUid uid, ChangelingComponent component, RegenerateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
        {
            return;
        }

        _rejuvenate.PerformRejuvenate(args.Target.Value);

        _popup.PopupEntity("We're fully regenerated!",  args.Target.Value);

        component.IsRegenerating = false;

        _action.StartUseDelay(component.RegenerateAction);

        args.Handled = true;
    }

    private void OnLesserFormDoAfter(EntityUid uid, ChangelingComponent component, LesserFormDoAfterEvent args)
    {
        if(args.Handled || args.Cancelled)
            return;

        var polymorphEntity = _polymorph.PolymorphEntity(args.User, "MonkeyChangeling");

        if(polymorphEntity == null)
            return;

        if (!EnsureComp<ChangelingComponent>(polymorphEntity.Value, out var polyChangeling))
        {
            polyChangeling.PointsBalance = component.PointsBalance;
            polyChangeling.ChemicalsBalance = component.ChemicalsBalance;
            polyChangeling.AbsorbedEntities = component.AbsorbedEntities;
            polyChangeling.IsLesserForm = true;
        }

        _action.RemoveAction(polyChangeling.RegenerateAction);
        _action.RemoveAction(polyChangeling.AbsorbAction);
        _action.RemoveAction(polyChangeling.LesserFormAction);
        _action.RemoveAction(polyChangeling.TransformStingAction);

        Dirty(polymorphEntity.Value, polyChangeling);

        args.Handled = true;
    }

    #endregion

    #region Helpers

    private void KillUser(EntityUid target, string damageType)
    {
        if (!_mobThresholdSystem.TryGetThresholdForState(target, MobState.Dead, out var damage))
            return;

        DamageSpecifier dmg = new();
        dmg.DamageDict.Add(damageType, damage.Value);
        _damage.TryChangeDamage(target, dmg, true);
    }

    private void CopyHumanoidData(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        if (!TryComp<MetaDataComponent>(target, out var targetMeta))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var targetAppearance))
            return;
        if (!TryComp<DnaComponent>(target, out var targetDna))
            return;
        if (!TryPrototype(target, out var prototype, targetMeta))
            return;

        if (component.AbsorbedEntities.ContainsKey(prototype.Name))
            return;

        var appearance = _serializationManager.CreateCopy(targetAppearance, notNullableOverride: true);
        var meta = _serializationManager.CreateCopy(targetMeta, notNullableOverride: true);

        component.AbsorbedEntities.Add(targetDna.DNA, new HumanoidData
        {
            MetaDataComponent = meta,
            AppearanceComponent = appearance,
            Name = meta.EntityName,
            Dna = targetDna.DNA
        });

        Dirty(uid, component);
    }

    private void TransformPerson(EntityUid target, HumanoidData transformData)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var appearance))
            return;

        ClonePerson(target, transformData.AppearanceComponent, appearance);
        TransferDna(target, transformData.Dna);

        if (!TryComp<MetaDataComponent>(target, out var meta))
            return;

        _metaData.SetEntityName(target, transformData.MetaDataComponent!.EntityName, meta);
        _metaData.SetEntityDescription(target, transformData.MetaDataComponent!.EntityDescription, meta);

        _identity.QueueIdentityUpdate(target);
    }

    private void TransferDna(EntityUid target, string dna)
    {
        if(!TryComp<DnaComponent>(target, out var dnaComponent))
            return;

        dnaComponent.DNA = dna;
    }

    private void TryTransformChangeling(EntityUid uid, string dna, ChangelingComponent component)
    {
        if (!component.AbsorbedEntities.TryGetValue(dna, out var person))
            return;

        EntityUid? reverted = uid;

        if (component.IsLesserForm)
        {
            reverted = _polymorph.Revert(uid);

            if(!TryComp<ChangelingComponent>(reverted, out var revertedComp))
                return;

            revertedComp.IsLesserForm = false;

            _action.StartUseDelay(revertedComp.LesserFormAction);
        }

        TransformPerson(reverted.Value, person);
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
