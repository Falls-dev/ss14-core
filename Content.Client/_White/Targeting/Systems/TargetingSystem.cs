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
    public event Action<TargetingBodyParts>? TargetChange;
    public event Action<TargetingComponent>? PartStatusStartup;
    public event Action<TargetingComponent>? PartStatusUpdate;
    public event Action? PartStatusShutdown;
    public override void Initialize()
    {
        base.Initialize();

        // Local Events Subscribers
        SubscribeLocalEvent<TargetingComponent, LocalPlayerAttachedEvent>(HandlePlayerAttached);
        SubscribeLocalEvent<TargetingComponent, LocalPlayerDetachedEvent>(HandlePlayerDetached);
        SubscribeLocalEvent<TargetingComponent, ComponentStartup>(OnTargetingStartup);
        SubscribeLocalEvent<TargetingComponent, ComponentShutdown>(OnTargetingShutdown);

        // Network Events Subscribers
        SubscribeNetworkEvent<TargetingIntegrityChangeEvent>(OnTargetingIntegrityChange);
    }

    private void HandlePlayerAttached(EntityUid uid, TargetingComponent component, LocalPlayerAttachedEvent args)
    {
        TargetingStartup?.Invoke(component);
        PartStatusStartup?.Invoke(component);
    }

    private void HandlePlayerDetached(EntityUid uid, TargetingComponent component, LocalPlayerDetachedEvent args)
    {
        TargetingShutdown?.Invoke();
        PartStatusShutdown?.Invoke();
    }

    private void OnTargetingStartup(EntityUid uid, TargetingComponent component, ComponentStartup args)
    {
        if (_playerManager.LocalEntity == uid)
        {
            TargetingStartup?.Invoke(component);
            PartStatusStartup?.Invoke(component);
        }
    }

    private void OnTargetingShutdown(EntityUid uid, TargetingComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == uid)
        {
            TargetingShutdown?.Invoke();
            PartStatusShutdown?.Invoke();
        }
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

    private void HandleTargetingChange(ICommonSession? session, TargetingBodyParts target)
    {
        if (session is not { AttachedEntity: { } uid } || !TryComp<TargetingComponent>(uid, out _))
            return;

        TargetChange?.Invoke(target);
    }
}
