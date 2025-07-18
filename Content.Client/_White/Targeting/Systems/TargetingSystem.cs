using Content.Shared._White.Targeting;
using Content.Shared._White.Targeting.Components;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._White.Targeting.Systems;

public sealed class TargetingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action<TargetingComponent>? TargetingStartup;
    public event Action? TargetingShutdown;
    public event Action<TargetingComponent>? PartStatusStartup;
    public event Action<TargetingComponent>? PartStatusUpdate;
    public event Action? PartStatusShutdown;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetingComponent, LocalPlayerAttachedEvent>(PlayerAttached);
        SubscribeLocalEvent<TargetingComponent, LocalPlayerDetachedEvent>(PlayerDetached);

        SubscribeLocalEvent<TargetingComponent, ComponentStartup>(OnTargetingStartup);
        SubscribeLocalEvent<TargetingComponent, ComponentShutdown>(OnTargetingShutdown);

        SubscribeNetworkEvent<TargetingIntegrityChangeEvent>(OnTargetingIntegrityChange);
    }

    private void PlayerAttached(EntityUid uid, TargetingComponent component, LocalPlayerAttachedEvent args)
    {
        TargetingStartup?.Invoke(component);
        PartStatusStartup?.Invoke(component);
    }

    private void PlayerDetached(EntityUid uid, TargetingComponent component, LocalPlayerDetachedEvent args)
    {
        TargetingShutdown?.Invoke();
        PartStatusShutdown?.Invoke();
    }

    private void OnTargetingStartup(EntityUid uid, TargetingComponent component, ComponentStartup args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        TargetingStartup?.Invoke(component);
        PartStatusStartup?.Invoke(component);
    }

    private void OnTargetingShutdown(EntityUid uid, TargetingComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        TargetingShutdown?.Invoke();
        PartStatusShutdown?.Invoke();
    }

    private void OnTargetingIntegrityChange(TargetingIntegrityChangeEvent args)
    {
        if(!TryGetEntity(args.Entity, out var uid))
            return;

        if(!TryComp(uid, out TargetingComponent? component))
               return;

        if (!_playerManager.LocalEntity.Equals(uid) || !args.NeedRefresh)
            return;

        PartStatusUpdate?.Invoke(component);
    }
}
