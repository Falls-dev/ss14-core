using System.Linq;
using System.Net.Http;
using Content.Client._Amour.Gif.UI;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Shared._Amour.Gif.Background;
using Content.Shared._White;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Amour.Gif.Background;

public sealed class LobbyBackgroundManager
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    private GifRect? _currentBackground;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);

        _netMgr.RegisterNetMessage<MsgRequestBackground>();
        _netMgr.RegisterNetMessage<MsgResponseBackground>(OnResponse);

        _stateManager.OnStateChanged += OnStateChanged;
    }

    private void OnResponse(MsgResponseBackground message)
    {
        if(!string.IsNullOrEmpty(message.Path.ToString()))
            SetBackground(message.Path);
        else
            RandomizeBackground();
    }

    private void OnStateChanged(StateChangedEventArgs obj)
    {
        if (obj.OldState is LobbyState)
        {
            _currentBackground = null;
            return;
        }

        if(obj.NewState is not LobbyState || _userInterfaceManager.ActiveScreen is not LobbyGui lobbyGui)
            return;

        _currentBackground = lobbyGui.Background;
        RequireBackgroundFromServer();
    }

    public void SetBackground(ProtoId<AnimatedLobbyScreenPrototype> protoId)
    {
        if(_prototypeManager.TryIndex(protoId, out var prototype))
            SetBackground(prototype);
    }

    public void SetBackground(AnimatedLobbyScreenPrototype prototype)
    {
        SetBackground(new ResPath(prototype.Path));
    }

    public void SetBackground(ResPath path)
    {
        if (_currentBackground != null)
            _currentBackground.GifPath = path.ToString();
    }

    public void RandomizeBackground()
    {
        var backgroundsProto = _prototypeManager.EnumeratePrototypes<AnimatedLobbyScreenPrototype>().ToList();
        var random = new Random();
        var index = random.Next(backgroundsProto.Count);
        SetBackground(backgroundsProto[index]);
    }

    public void RequireBackgroundFromServer()
    {
        _netMgr.ClientSendMessage(new MsgRequestBackground());
    }
}
