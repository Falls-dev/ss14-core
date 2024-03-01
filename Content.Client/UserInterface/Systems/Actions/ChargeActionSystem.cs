using Content.Client.Actions;
using Content.Shared.Actions;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed class ChargeActionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private ActionUIController? _controller;

    private bool _charging;
    private float _chargeTime;
    private int _chargeLevel;

    private const float LevelChargeTime = 1.5f;

    public override void Initialize()
    {
        base.Initialize();

        _controller = _uiManager.GetUIController<ActionUIController>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || _controller == null || _controller.SelectingTargetFor is not { } actionId)
            return;

        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);
        switch (altDown)
        {
            case BoundKeyState.Down:
                _charging = true;
                _chargeTime += frameTime;
                _chargeLevel = (int)(_chargeTime / LevelChargeTime) + 1;
                _chargeLevel = Math.Clamp(_chargeLevel, 1, 4);
                break;
            case BoundKeyState.Up when _charging:
                _charging = false;
                _chargeTime = 0f;
                HandleAction(actionId);
                break;
        }
    }

    private void HandleAction(EntityUid actionId)
    {
        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        var coordinates = EntityCoordinates.FromMap(_mapManager.TryFindGridAt(mousePos, out var gridUid, out _)
            ? gridUid
            : _mapManager.GetMapEntityId(mousePos.MapId), mousePos, _transformSystem, EntityManager);

        if (_playerManager.LocalEntity is not { } user)
            return;

        if (!EntityManager.TryGetComponent(user, out ActionsComponent? comp))
            return;

        if (!_actionsSystem.TryGetActionData(actionId, out var baseAction) ||
            baseAction is not BaseTargetActionComponent action)
        {
            return;
        }

        if (!action.Enabled
            || action is { Charges: 0, RenewCharges: false }
            || action.Cooldown.HasValue && action.Cooldown.Value.End > _timing.CurTime)
        {
            return;
        }

        switch (action)
        {
            case WorldTargetActionComponent mapTarget:
                _controller?.TryTargetWorld(coordinates, actionId, mapTarget, user, comp, ActionUseType.Charge, _chargeLevel);
                break;
        }
    }
}
