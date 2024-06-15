using Content.Server.Cloning;
using Content.Server.Cloning.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Genetics.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DNAModifier;
using Content.Shared.DragDrop;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using static Content.Shared.DNAModifier.SharedDNAModifierComponent;

namespace Content.Server.Genetics;

public sealed class DNAModifierSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly CloningConsoleSystem _cloningConsoleSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const float UpdateRate = 1f;
    private float _updateDif;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DNAModifierComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DNAModifierComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<DNAModifierComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
        SubscribeLocalEvent<DNAModifierComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<DNAModifierComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<DNAModifierComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<DNAModifierComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<DNAModifierComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<DNAModifierComponent, CanDropTargetEvent>(OnCanDragDropOn);
    }

    private void OnCanDragDropOn(EntityUid uid, DNAModifierComponent component, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop |= CanModifierInsert(uid, args.Dragged, component);
    }
    public bool CanModifierInsert(EntityUid uid, EntityUid target, DNAModifierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return HasComp<BodyComponent>(target);
    }

    private void OnComponentInit(EntityUid uid, DNAModifierComponent scannerComponent, ComponentInit args)
    {
        base.Initialize();
        scannerComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"scanner-bodyContainer");
        _signalSystem.EnsureSinkPorts(uid, DNAModifierComponent.ScannerPort);
    }

    private void OnRelayMovement(EntityUid uid, DNAModifierComponent scannerComponent, ref ContainerRelayMovementEntityEvent args)
    {
        if (!_blocker.CanInteract(args.Entity, uid))
            return;

        EjectBody(uid, scannerComponent);
    }
    private void AddInsertOtherVerb(EntityUid uid, DNAModifierComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Using == null ||
            !args.CanAccess ||
            !args.CanInteract ||
            IsOccupied(component) ||
            !CanModifierInsert(uid, args.Using.Value, component))
            return;

        var name = "Unknown";
        if (TryComp<MetaDataComponent>(args.Using.Value, out var metadata))
            name = metadata.EntityName;

        InteractionVerb verb = new()
        {
            Act = () => InsertBody(uid, args.Target, component),
            Category = VerbCategory.Insert,
            Text = name
        };
        args.Verbs.Add(verb);
    }
    private void AddAlternativeVerbs(EntityUid uid, DNAModifierComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject verb
        if (IsOccupied(component))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(uid, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("dna-modifier-verb-noun-occupant"),
                Priority = 1 // Promote to top to make ejecting the ALT-click action
            };
            args.Verbs.Add(verb);
        }
    }
    private void OnDestroyed(EntityUid uid, DNAModifierComponent scannerComponent, DestructionEventArgs args)
    {
        EjectBody(uid, scannerComponent);
    }

    private void OnDragDropOn(EntityUid uid, DNAModifierComponent scannerComponent, ref DragDropTargetEvent args)
    {
        InsertBody(uid, args.Dragged, scannerComponent);

    }
    private void OnPortDisconnected(EntityUid uid, DNAModifierComponent component, PortDisconnectedEvent args)
    {
        component.ConnectedConsole = null;
    }

    private void OnAnchorChanged(EntityUid uid, DNAModifierComponent component, ref AnchorStateChangedEvent args)
    {
        if (component.ConnectedConsole == null || !TryComp<DNAConsoleComponent>(component.ConnectedConsole, out var console))
            return;

        if (args.Anchored)
        {
            _cloningConsoleSystem.RecheckConnections(component.ConnectedConsole.Value, uid, console);
            return;
        }
        _cloningConsoleSystem.UpdateUserInterface(component.ConnectedConsole.Value, console);
    }
    private DNAModifierStatus GetStatus(EntityUid uid, DNAModifierComponent scannerComponent)
    {
        if (this.IsPowered(uid, EntityManager))
        {
            var body = scannerComponent.BodyContainer.ContainedEntity;
            if (body == null)
                return DNAModifierStatus.Open;

            if (!TryComp<MobStateComponent>(body.Value, out var state))
            {   // Is not alive or dead or critical
                return DNAModifierStatus.Yellow;
            }

            return GetStatusFromDamageState(body.Value, state);
        }
        return DNAModifierStatus.Off;
    }
    public static bool IsOccupied(DNAModifierComponent scannerComponent)
    {
        return scannerComponent.BodyContainer.ContainedEntity != null;
    }
    private DNAModifierStatus GetStatusFromDamageState(EntityUid uid, MobStateComponent state)
    {
        if (_mobStateSystem.IsAlive(uid, state))
            return DNAModifierStatus.Green;

        if (_mobStateSystem.IsCritical(uid, state))
            return DNAModifierStatus.Red;

        if (_mobStateSystem.IsDead(uid, state))
            return DNAModifierStatus.Death;

        return DNAModifierStatus.Yellow;
    }
    private void UpdateAppearance(EntityUid uid, DNAModifierComponent scannerComponent)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, DNAModifierVisual.Status, GetStatus(uid, scannerComponent), appearance);
        }
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateDif += frameTime;
        if (_updateDif < UpdateRate)
            return;

        _updateDif -= UpdateRate;

        var query = EntityQueryEnumerator<DNAModifierComponent>();
        while (query.MoveNext(out var uid, out var scanner))
        {
            UpdateAppearance(uid, scanner);
        }
    }
    public void InsertBody(EntityUid uid, EntityUid to_insert, DNAModifierComponent? scannerComponent)
    {
        if (!Resolve(uid, ref scannerComponent))
            return;

        if (scannerComponent.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<BodyComponent>(to_insert))
            return;

        if(HasComp<HumanoidAppearanceComponent>(to_insert))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(uid) && !HasComp<MonkeyAccentComponent>(to_insert))
            return;

        _containerSystem.Insert(to_insert, scannerComponent.BodyContainer);
        AddComp<ActiveModifierComponent>(to_insert);
        UpdateAppearance(uid, scannerComponent);
    }
    public void EjectBody(EntityUid uid, DNAModifierComponent? scannerComponent)
    {
        if (!Resolve(uid, ref scannerComponent))
            return;

        if (scannerComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return;

        _containerSystem.Remove(contained, scannerComponent.BodyContainer);
        _climbSystem.ForciblySetClimbing(contained, uid);
        RemCompDeferred<ActiveModifierComponent>(contained);
        UpdateAppearance(uid, scannerComponent);
    }
}
