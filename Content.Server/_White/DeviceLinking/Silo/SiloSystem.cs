using Content.Server._White.DeviceLinking.Silo.Components;
using Content.Server.DeviceLinking.Events;

namespace Content.Server._White.DeviceLinking.Silo;

public sealed class SiloSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BluespaceMaterialStorageComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(EntityUid uid, BluespaceMaterialStorageComponent component, SignalReceivedEvent args)
    {

    }
}
