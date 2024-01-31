using Content.Client.Actions;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Actions.Windows;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Actions;
using Content.Shared.Borer;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client.Borer;


public sealed class BorerScannerUIController : UIController, IOnSystemChanged<ActionsSystem>
{
    // Dependency is used for IoC services and other controllers
    [Dependency] private readonly GameplayStateLoadController _gameplayStateLoad = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ScannerWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        // We can bind methods to event fields on other UI controllers during initialize
        _gameplayStateLoad.OnScreenLoad += LoadGui;
        _gameplayStateLoad.OnScreenUnload += UnloadGui;

        // UI controllers can also subscribe to local and network entity events
        // Local events are events raised on the client using RaiseLocalEvent
        SubscribeNetworkEvent<BorerScanDoAfterEvent>(OpenWindow);

    }

    private void LoadGui()
    {
        DebugTools.Assert(_window == null);
        _window = UIManager.CreateWindow<ScannerWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);
    }

    private void UnloadGui()
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }
    }
    private void OpenWindow(BorerScanDoAfterEvent msg, EntitySessionEventArgs args)
    {
        var ent = _playerManager.LocalPlayer?.ControlledEntity;
        if (_window == null || _window.IsOpen || ent != args.SenderSession.AttachedEntity)
            return;
        _window.SolutionContainer.DisposeAllChildren();
        foreach (var reagent in msg.Solution)
        {
            var reagLabel = new Label();
            reagLabel.Text = reagent.Key + " - " + reagent.Value + "u";
            _window.SolutionContainer.Children.Add(reagLabel);
        }

        _window.Open();
        // _playerManager.LocalPlayer?.ControlledEntity
    }


    public void OnSystemLoaded(ActionsSystem system)
    {
        // We can bind to event fields on entity systems when that entity system is loaded
        system.LinkActions += OnComponentLinked;
    }

    public void OnSystemUnloaded(ActionsSystem system)
    {
        // And unbind when the system is unloaded
        system.LinkActions -= OnComponentLinked;
    }

    // This will be called when ActionsSystem raises an event on its LinkActions event field
    private void OnComponentLinked(ActionsComponent component)
    {
    }

}
