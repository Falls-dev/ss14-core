using System.Linq;
using Content.Server._White.CartridgeLoader.Cartridges;
using Content.Server._White.Radio.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared._White.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.DeviceNetwork;


namespace Content.Server._White.Radio.EntitySystems;

public sealed class MessagesServerSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly MessagesCartridgeSystem _messagesSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagesServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<MessagesServerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, MessagesServerComponent component, ComponentInit args)
    {
        var query = EntityQueryEnumerator<MessagesCartridgeComponent>();

        while (query.MoveNext(out var entityUid, out var cartridge))
        {
            if (!TryComp(entityUid, out CartridgeComponent? cartComponent))
                continue;

            _messagesSystem.TryGetUserName(cartComponent, out var name);

            if (cartridge.UserUid == null)
                continue;

            component.NameDict[cartridge.UserUid.Value] = name;
            cartridge.LastServer = uid;
        }
    }

    /// <summary>
    /// Reacts to packets received from clients
    /// </summary>
    private void OnPacketReceived(EntityUid uid, MessagesServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!_singletonServerSystem.IsActiveServer(uid))
            return;
        if (args.Data.TryGetValue<string>(MessagesNetworkKeys.NewName, out var name) && args.Data.TryGetValue<int>(MessagesNetworkKeys.UserId, out var userId))
        {
            component.NameDict[userId] = name;

            var packet = new NetworkPayload();
            _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, packet);
        }
        if (args.Data.TryGetValue<MessagesMessageData>(MessagesNetworkKeys.Message, out var message))
            SendMessage(uid, component, message);
    }

    /// <summary>
    /// Broadcasts a message into the network
    /// </summary>
    private void SendMessage(EntityUid uid, MessagesServerComponent component, MessagesMessageData message)
    {
        component.Messages.Add(message);

        var packet = new NetworkPayload()
        {
            [MessagesNetworkKeys.Message] = message
        };

        _deviceNetworkSystem.QueuePacket(uid, null, packet);
    }

    /// <summary>
    /// Returns the name of a given user
    /// </summary>
    public bool TryGetNameFromDict(EntityUid? uid, int key, out string name)
    {
        if (!TryComp(uid, out MessagesServerComponent? component))
        {
            name = Loc.GetString("messages-pda-connection-error");
            return false;
        }
        if (component.NameDict.TryGetValue(key, out var keyValue))
        {
            name = keyValue;
            return true;
        }
        name = Loc.GetString("messages-pda-user-missing");
        return false;
    }

    /// <summary>
    /// Returns the name dictionary cache
    /// </summary>
    public Dictionary<int, string> GetNameDict(EntityUid? uid)
    {
        if (!TryComp(uid, out MessagesServerComponent? component))
            return new Dictionary<int, string>();
        return component.NameDict;
    }

    /// <summary>
    /// Returns list of messages between the two users
    /// </summary>
    public List<MessagesMessageData> GetMessages(EntityUid? uid, int id1, int id2)
    {
        if (!TryComp(uid, out MessagesServerComponent? component))
            return [];
        return
        [
            ..component.Messages.Where(message =>
                message.SenderId == id1 && message.ReceiverId == id2 ||
                message.SenderId == id2 && message.ReceiverId == id1)
        ];
    }

}

public static class MessagesNetworkKeys
{
    public const string NewName = "new_name";
    public const string UserId = "user_id";
    public const string Message = "message";
}
