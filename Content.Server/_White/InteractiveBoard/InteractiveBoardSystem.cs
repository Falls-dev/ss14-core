using Content.Server._White.Other;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using static Content.Shared._White.InteractiveBoard.SharedInteractiveBoardComponent;

namespace Content.Server._White.InteractiveBoard;

public sealed class InteractiveBoardSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractiveBoardComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<InteractiveBoardComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<InteractiveBoardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<InteractiveBoardComponent, InteractiveBoardInputTextMessage>(OnInputTextMessage);

        SubscribeLocalEvent<InteractiveBoardComponent, BeforeRangedInteractEvent>(BeforeRangedInteract);

        SubscribeLocalEvent<ActivateOnInteractiveBoardOpenedComponent, InteractiveBoardWriteEvent>(OnInteractiveBoardWrite);

        SubscribeLocalEvent<InteractiveBoardComponent, MapInitEvent>(OnMapInit);
    }

        private void OnMapInit(EntityUid uid, InteractiveBoardComponent component, MapInitEvent args)
        {
            if (!string.IsNullOrEmpty(component.Content))
            {
                component.Content = Loc.GetString(component.Content);
            }
        }

        private void OnInit(EntityUid uid, InteractiveBoardComponent component, ComponentInit args)
        {
            component.Mode = InteractiveBoardAction.Read;
            UpdateUserInterface(uid, component);

            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            if (component.Content != "")
                _appearance.SetData(uid, InteractiveBoardVisuals.Status, InteractiveBoardStatus.Written, appearance);
        }

        private void BeforeUIOpen(EntityUid uid, InteractiveBoardComponent component, BeforeActivatableUIOpenEvent args)
        {
            component.Mode = InteractiveBoardAction.Read;

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            UpdateUserInterface(uid, component, actor.PlayerSession);
        }

        private void BeforeRangedInteract(EntityUid uid, InteractiveBoardComponent component, BeforeRangedInteractEvent args)
        {
            if (_tagSystem.HasTag(args.Used, "InteractiveBoard"))
            {
                if (!HasComp<WallMarkComponent>(args.Target) && !HasComp<WindowMarkComponent>(args.Target))
                    return;

                if(!TryComp<TransformComponent>(args.Target, out var transformComponent))
                    return;

                if (!TryComp<TransformComponent>(args.Used, out var xform))
                    return;

                _handsSystem.TryDrop(args.User, args.Used);

                _transformSystem.SetCoordinates(args.Used, transformComponent.Coordinates);
                _transformSystem.AnchorEntity(args.Used, xform);
                _transformSystem.AttachToGridOrMap(args.Used, xform);

                AddComp<WallMountComponent>(args.Used).Arc = new Angle(360);
            }
        }

        private void OnInteractUsing(EntityUid uid, InteractiveBoardComponent component, InteractUsingEvent args)
        {
            if (!_tagSystem.HasTag(args.Used, "InteractivePen"))
                return;

            if(!TryComp<AccessReaderComponent>(args.Target, out var accessReaderComponent))
                return;

            if (!_accessReaderSystem.IsAllowed(args.User, args.Target, accessReaderComponent))
            {
                _popupSystem.PopupEntity(Loc.GetString("interactive-board-not-allowed"), args.User, args.User, PopupType.Medium);
                return;
            }

            var writeEvent = new InteractiveBoardWriteEvent(uid, args.User);
            RaiseLocalEvent(args.Used, ref writeEvent);

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            component.Mode = InteractiveBoardAction.Write;
            _uiSystem.TryOpen(uid, InteractiveBoardUiKey.Key, actor.PlayerSession);
            UpdateUserInterface(uid, component, actor.PlayerSession);
            args.Handled = true;
        }

        private void OnInputTextMessage(EntityUid uid, InteractiveBoardComponent component, InteractiveBoardInputTextMessage args)
        {
            if (args.Text.Length <= component.ContentSize)
            {
                component.Content = args.Text;

                if (component.Content.Length == 0)
                    return;

                if (TryComp<AppearanceComponent>(uid, out var appearance))
                    _appearance.SetData(uid, InteractiveBoardVisuals.Status, InteractiveBoardStatus.Written, appearance);
            }

            component.Mode = InteractiveBoardAction.Read;
            UpdateUserInterface(uid, component);
        }

        private void OnInteractiveBoardWrite(EntityUid uid, ActivateOnInteractiveBoardOpenedComponent comp, ref InteractiveBoardWriteEvent args)
        {
            _interaction.UseInHandInteraction(args.User, uid);
        }

        public void SetContent(EntityUid uid, string content, InteractiveBoardComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Content = content + '\n';
            UpdateUserInterface(uid, component);

            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            var status = string.IsNullOrWhiteSpace(content)
                ? InteractiveBoardStatus.Blank
                : InteractiveBoardStatus.Written;

            _appearance.SetData(uid, InteractiveBoardVisuals.Status, status, appearance);
        }

        public void UpdateUserInterface(EntityUid uid, InteractiveBoardComponent? component = null, ICommonSession? session = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (_uiSystem.TryGetUi(uid, InteractiveBoardUiKey.Key, out var bui))
                _uiSystem.SetUiState(bui, new InteractiveBoardBoundUserInterfaceState(component.Content, component.Mode), session);
        }
}

[ByRefEvent]
public record struct InteractiveBoardWriteEvent(EntityUid User, EntityUid InteractiveBoard);
