using Content.Shared._White.Cult.Components;
using Content.Shared._White.Cult.Interfaces;

namespace Content.Shared._White.Cult.Systems;

/// <summary>
/// Thats need for chat perms update
/// </summary>
public sealed class CultistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConstructComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ConstructComponent, ComponentShutdown>(OnRemove);
        SubscribeLocalEvent<CultistComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<CultistComponent, ComponentShutdown>(OnRemove);
    }

    private void OnInit(EntityUid uid, ICultChat component, ComponentStartup args)
    {
        RaiseLocalEvent(new EventCultistComponentState(true));
    }

    private void OnRemove(EntityUid uid, ICultChat component, ComponentShutdown args)
    {
        RaiseLocalEvent(new EventCultistComponentState(false));
    }
}

public sealed class EventCultistComponentState
{
    public bool Created { get; }
    public EventCultistComponentState(bool state)
    {
        Created = state;
    }
}
