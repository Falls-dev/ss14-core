using Content.Server.Humanoid;
using Content.Server.Preferences.Managers;
using Content.Shared._White.Wizard.Mirror;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Preferences;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._White.Wizard.Mirror;

public sealed class WizardMirrorSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WizardMirrorComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<WizardMirrorComponent>(WizardMirrorUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIClosedEvent>(OnUIClosed);
                subs.Event<WizardMirrorSave>(OnSave);
            });

        SubscribeLocalEvent<WizardMirrorComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<WizardMirrorComponent, AfterInteractEvent>(OnMagicMirrorInteract);

        SubscribeLocalEvent<WizardMirrorComponent, BoundUserInterfaceCheckRangeEvent>(OnRangeCheck);
    }

    private void OnOpenUIAttempt(EntityUid uid, WizardMirrorComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.User))
            args.Cancel();
    }

    private static void OnUIClosed(Entity<WizardMirrorComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
    }

    private void OnSave(EntityUid uid, WizardMirrorComponent component, WizardMirrorSave args)
    {
        if (!TryComp(component.Target, out HumanoidAppearanceComponent? humanoid) || !string.IsNullOrEmpty(humanoid.Initial))
            return;

        _humanoid.LoadProfile(component.Target.Value, args.Profile, humanoid);
        _metaData.SetEntityName(component.Target.Value, args.Profile.Name);
    }

    private void OnInteractHand(EntityUid uid, WizardMirrorComponent component, ref InteractHandEvent args)
    {
        UpdateInterface(uid, args.User, component);
    }

    private void OnMagicMirrorInteract(EntityUid uid, WizardMirrorComponent component, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (!_uiSystem.TryOpen(uid, WizardMirrorUiKey.Key, actor.PlayerSession))
            return;

        UpdateInterface(uid, args.Target.Value, component);
    }

    private void OnRangeCheck(EntityUid uid, WizardMirrorComponent component, ref BoundUserInterfaceCheckRangeEvent args)
    {
        component.Target ??= args.Player.AttachedEntity;

        if (!component.Target.HasValue || !_interaction.InRangeUnobstructed(uid, component.Target!.Value, range: 2f, CollisionGroup.None))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void UpdateInterface(EntityUid mirrorUid, EntityUid targetUid, WizardMirrorComponent component)
    {
        if (!TryComp<ActorComponent>(targetUid, out var actor))
            return;

        var profile = (HumanoidCharacterProfile) _prefs.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter;

        var state = new WizardMirrorUiState(profile);

        component.Target = targetUid;
        _uiSystem.TrySetUiState(mirrorUid, WizardMirrorUiKey.Key, state);
    }
}
