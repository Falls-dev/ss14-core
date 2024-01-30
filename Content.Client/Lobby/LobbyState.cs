using Content.Client._Ohio.Buttons;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.Preferences;
using Content.Client.Preferences.UI;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.Voting;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;


namespace Content.Client.Lobby
{
    public sealed class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        [ViewVariables] private CharacterSetupGui? _characterSetup;

        private ClientGameTicker _gameTicker = default!;

        protected override Type? LinkedScreenType { get; } = typeof(LobbyGui);
        private LobbyGui? _lobby;

        protected override void Startup()
        {
            if (_userInterfaceManager.ActiveScreen == null)
            {
                return;
            }

            _lobby = (LobbyGui) _userInterfaceManager.ActiveScreen;

            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();

            _gameTicker = _entityManager.System<ClientGameTicker>();

            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager, _prototypeManager, _configurationManager);

            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);

            _lobby.CharacterSetupState.AddChild(_characterSetup);

            chatController.SetMainChat(true);

            _voteManager.SetPopupContainer(_lobby.VoteContainer);

            _characterSetup.CloseButton.OnPressed += _ =>
            {
                _lobby.SwitchState(LobbyGui.LobbyGuiState.Default);
            };

            _characterSetup.SaveButton.OnPressed += _ =>
            {
                _characterSetup.Save();
                _lobby.CharacterPreview.UpdateUI();
            };

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);

            _lobby.ServerName.Text = _baseClient.GameInfo?.ServerName; //The eye of refactor gazes upon you...

            UpdateLobbyUi();

            _lobby.CharacterSetupButton.OnPressed += OnSetupPressed;
            _lobby.ReadyButton.OnPressed += OnReadyPressed;
            _lobby.ReadyButton.OnToggled += OnReadyToggled;

            _gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;

            _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;

            _lobby.CharacterPreview.UpdateUI();
        }

        protected override void Shutdown()
        {
            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();

            chatController.SetMainChat(false);

            _gameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _gameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;

            _voteManager.ClearPopupContainer();

            _lobby!.CharacterSetupButton.OnPressed -= OnSetupPressed;
            _lobby!.ReadyButton.OnPressed -= OnReadyPressed;
            _lobby!.ReadyButton.OnToggled -= OnReadyToggled;

            _lobby = null;

            _characterSetup?.Dispose();
            _characterSetup = null;

            _preferencesManager.OnServerDataLoaded -= PreferencesDataLoaded;
        }

        private void PreferencesDataLoaded()
        {
            _lobby?.CharacterPreview.UpdateUI();
        }

        private void OnSetupPressed(BaseButton.ButtonEventArgs args)
        {
            SetReady(false);
            _lobby!.SwitchState(LobbyGui.LobbyGuiState.CharacterSetup);
        }

        private void OnReadyPressed(BaseButton.ButtonEventArgs args)
        {
            if (!_gameTicker.IsGameStarted)
            {
                return;
            }

            new LateJoinGui().OpenCentered();
        }

        private void OnReadyToggled(BaseButton.ButtonToggledEventArgs args)
        {
            SetReady(args.Pressed);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_gameTicker.IsGameStarted)
            {
                _lobby!.StartTime.Text = string.Empty;
                var roundTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                _lobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-time", ("hours", roundTime.Hours), ("minutes", roundTime.Minutes));
                return;
            }

            _lobby!.StationTime.Text =  Loc.GetString("lobby-state-player-status-round-not-started");
            string text;

            if (_gameTicker.Paused)
            {
                text = Loc.GetString("lobby-state-paused");
            }
            else if (_gameTicker.StartTime < _gameTiming.CurTime)
            {
                _lobby!.StartTime.Text = Loc.GetString("lobby-state-soon");
                return;
            }
            else
            {
                var difference = _gameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                {
                    text = Loc.GetString(seconds < -5 ? "lobby-state-right-now-question" : "lobby-state-right-now-confirmation");
                }
                else
                {
                    text = $"{difference.Minutes}:{difference.Seconds:D2}";
                }
            }

            _lobby!.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void LobbyStatusUpdated()
        {
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            _lobby!.ReadyButton.Disabled = _gameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_gameTicker.IsGameStarted)
            {
                MakeButtonJoinGame(_lobby!.ReadyButton);
                _lobby!.ReadyButton.ToggleMode = false;
                _lobby!.ReadyButton.Pressed = false;
                _lobby!.ObserveButton.Disabled = false;
            }
            else
            {
                _lobby!.StartTime.Text = string.Empty;

                if (_lobby!.ReadyButton.Pressed)
                    MakeButtonReady(_lobby!.ReadyButton);
                else
                    MakeButtonUnReady(_lobby!.ReadyButton);

                _lobby!.ReadyButton.ToggleMode = true;
                _lobby!.ReadyButton.Disabled = false;
                _lobby!.ReadyButton.Pressed = _gameTicker.AreWeReady;
                _lobby!.ObserveButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
            {
                _lobby!.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);
            }

            _lobby!.LabelName.SetMarkup("[font=\"Bedstead\" size=20] Green Miracle [/font]");
            _lobby!.Version.SetMarkup("Version: 1.0");
        }

        private void SetReady(bool newReady)
        {
            if (_gameTicker.IsGameStarted)
            {
                return;
            }

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
        }

        private void MakeButtonReady(OhioLobbyTextButton button)
        {
            button.ButtonText = "Ready";
            button.Fraction = 3f;
        }

        private void MakeButtonUnReady(OhioLobbyTextButton button)
        {
            button.ButtonText = "UnReady";
            button.Fraction = 2.9f;
        }

        private void MakeButtonJoinGame(OhioLobbyTextButton button)
        {
            button.ButtonText = "Join Game";
            button.Fraction = 2.6f;
        }
    }
}
