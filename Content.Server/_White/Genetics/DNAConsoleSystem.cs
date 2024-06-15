using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Genetics.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.UserInterface;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Content.Shared.DNAConsole;

namespace Content.Server.Genetics
{
    [UsedImplicitly]
    public sealed class CloningConsoleSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DNAConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DNAConsoleComponent, UiButtonPressedMessage>(OnButtonPressed);
            SubscribeLocalEvent<DNAConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
            SubscribeLocalEvent<DNAConsoleComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<DNAConsoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DNAConsoleComponent, NewLinkEvent>(OnNewLink);
            SubscribeLocalEvent<DNAConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<DNAConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        }

        private void OnInit(EntityUid uid, DNAConsoleComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSourcePorts(uid, DNAConsoleComponent.ScannerPort);
        }
        private void OnButtonPressed(EntityUid uid, DNAConsoleComponent consoleComponent, UiButtonPressedMessage args)
        {
            if (!_powerReceiverSystem.IsPowered(uid))
                return;
/*
ПОСЛЕ ГЕНОВ
            switch (args.Button)
            {
                case UiButton.Clone:
                    if (consoleComponent.Modifier != null)
                        TryClone(uid,consoleComponent.Modifier.Value, consoleComponent: consoleComponent);
                    break;
            }
*/
            UpdateUserInterface(uid, consoleComponent);
        }

        private void OnPowerChanged(EntityUid uid, DNAConsoleComponent component, ref PowerChangedEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnMapInit(EntityUid uid, DNAConsoleComponent component, MapInitEvent args)
        {
            if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
                return;

            foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
            {
                if (TryComp<DNAModifierComponent>(port, out var scanner))
                {
                    component.Modifier = port;
                    scanner.ConnectedConsole = uid;
                }
            }
        }

        private void OnNewLink(EntityUid uid, DNAConsoleComponent component, NewLinkEvent args)
        {
            if (TryComp<DNAModifierComponent>(args.Sink, out var scanner) && args.SourcePort == DNAConsoleComponent.ScannerPort)
            {
                component.Modifier = args.Sink;
                scanner.ConnectedConsole = uid;
            }

            RecheckConnections(uid, component.Modifier, component);
        }

        private void OnPortDisconnected(EntityUid uid, DNAConsoleComponent component, PortDisconnectedEvent args)
        {
            if (args.Port == DNAConsoleComponent.ScannerPort)
                component.Modifier = null;

            UpdateUserInterface(uid, component);
        }

        private void OnUIOpen(EntityUid uid, DNAConsoleComponent component, AfterActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnAnchorChanged(EntityUid uid, DNAConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                RecheckConnections(uid, component.Modifier, component);
                return;
            }
            UpdateUserInterface(uid, component);
        }

        public void UpdateUserInterface(EntityUid consoleUid, DNAConsoleComponent consoleComponent)
        {
            if (!_uiSystem.TryGetUi(consoleUid, DNAConsoleUiKey.Key, out var ui))
                return;

            if (!_powerReceiverSystem.IsPowered(consoleUid))
            {
                _uiSystem.CloseAll(ui);
                return;
            }

            var newState = GetUserInterfaceState(consoleComponent);
            _uiSystem.SetUiState(ui, newState);
        }
/*
        public void TryGens(EntityUid uid, DNAModifierComponent? scannerComp = null, DNAConsoleComponent? consoleComponent = null)
        {
            логика на гены, будет после добавления самих генов
        }

*/

        public void RecheckConnections(EntityUid console, EntityUid? scanner, DNAConsoleComponent? consoleComp = null)
        {
            if (!Resolve(console, ref consoleComp))
                return;

            if (scanner != null)
            {
                Transform(scanner.Value).Coordinates.TryDistance(EntityManager, Transform((console)).Coordinates, out float scannerDistance);
                consoleComp.ModifierInRange = scannerDistance <= consoleComp.MaxDistance;
            }

            UpdateUserInterface(console, consoleComp);
        }
        private DNAConsoleBoundUserInterfaceState GetUserInterfaceState(DNAConsoleComponent consoleComponent)
        {
            ModifierStatus _modifierStatus = ModifierStatus.Ready;

            // modifier info
            string scanBodyInfo = Loc.GetString("generic-unknown");
            bool modifierConnected = false;
            bool modifierInRange = consoleComponent.ModifierInRange;
            if (consoleComponent.Modifier != null && TryComp<DNAModifierComponent>(consoleComponent.Modifier, out var scanner))
            {
                modifierConnected = true;
                EntityUid? scanBody = scanner.BodyContainer.ContainedEntity;

                // GET STATE
                if (scanBody == null || !HasComp<MobStateComponent>(scanBody))
                    _modifierStatus = ModifierStatus.ModifierEmpty;
                else
                {
                    scanBodyInfo = MetaData(scanBody.Value).EntityName;

                    if (!_mobStateSystem.IsDead(scanBody.Value))
                    {
                        _modifierStatus = ModifierStatus.ModifierOccupantAlive;
                    }
                }

                var modifierBodyInfo = Loc.GetString("generic-unknown");

                if (HasComp<ActiveModifierComponent>(consoleComponent.Modifier))
                {
                    if(scanBody != null)
                        modifierBodyInfo = Identity.Name(scanBody.Value, EntityManager);
                    _modifierStatus = ModifierStatus.ModifierOccupied;
                }
            }

            return new DNAConsoleBoundUserInterfaceState(
                scanBodyInfo,
                _modifierStatus,
                modifierConnected,
                modifierInRange
                );
        }

    }
}
