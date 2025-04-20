using Content.Client._White.Targeting.Systems;
using Content.Client._White.Targeting.Ui;
using Content.Client.Gameplay;
using Content.Shared._White.Targeting;
using Content.Shared._White.Targeting.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._White.Targeting;

public sealed class TargetingController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private TargetingComponent? _targetingComponent;
    private TargetingWidget? TargetingControl => UIManager.GetActiveUIWidgetOrNull<TargetingWidget>();

    public void OnSystemLoaded(TargetingSystem system)
    {
        system.TargetingStartup += AddTargetingControl;
        system.TargetingShutdown += RemoveTargetingControl;
        system.TargetChange += CycleTarget;
    }

    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.TargetingStartup -= AddTargetingControl;
        system.TargetingShutdown -= RemoveTargetingControl;
        system.TargetChange -= CycleTarget;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (TargetingControl == null)
            return;

        TargetingControl.SetTargetDollVisible(_targetingComponent != null);

        if (_targetingComponent != null)
            TargetingControl.SetBodyPartsVisible(_targetingComponent.TargetBodyPart);
    }

    public void AddTargetingControl(TargetingComponent component)
    {
        _targetingComponent = component;

        if (TargetingControl != null)
        {
            TargetingControl.SetTargetDollVisible(_targetingComponent != null);

            if (_targetingComponent != null)
                TargetingControl.SetBodyPartsVisible(_targetingComponent.TargetBodyPart);
        }

    }

    public void RemoveTargetingControl()
    {
        TargetingControl?.SetTargetDollVisible(false);

        _targetingComponent = null;
    }

    public void CycleTarget(TargetingBodyParts bodyPart)
    {
        if (_playerManager.LocalEntity is not { } user
            || _entManager.GetComponent<TargetingComponent>(user) is not { } targetingComponent
            || TargetingControl == null)
            return;

        var player = _entManager.GetNetEntity(user);

        if (bodyPart == targetingComponent.TargetBodyPart)
            return;

        var msg = new TargetingChangeBodyPartEvent(player, bodyPart);
        _net.SendSystemNetworkMessage(msg);
        TargetingControl?.SetBodyPartsVisible(bodyPart);
    }
}
