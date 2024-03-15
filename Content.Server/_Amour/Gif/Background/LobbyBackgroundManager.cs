using Content.Server.Administration;
using Content.Shared._Amour.Gif.Background;
using Content.Shared._White;
using Content.Shared.Administration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._Amour.Gif.Background;

public sealed class LobbyBackgroundManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;

    public ResPath Path { get; private set;  }

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgRequestBackground>(OnRequest);
        _netMgr.RegisterNetMessage<MsgResponseBackground>();
    }

    private void OnRequest(MsgRequestBackground message)
    {
        message.MsgChannel.SendMessage(new MsgResponseBackground()
        {
            Path = Path
        });
    }

    public void SetBackground(ProtoId<AnimatedLobbyScreenPrototype> protoId)
    {
        if (!_prototypeManager.TryIndex(protoId, out var prototype))
            return;

        SetBackground(new ResPath(prototype.Path));
    }

    public void SetBackground(AnimatedLobbyScreenPrototype prototype)
    {
        SetBackground(prototype.ID);
    }

    public void SetBackground(ResPath resPath)
    {
        Path = resPath;
        _netMgr.ServerSendToAll(new MsgResponseBackground()
        {
            Path = resPath
        });
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
internal sealed class SetLobbyScreenCommand : ToolshedCommand
{
    [Dependency] private readonly LobbyBackgroundManager _lobbyBackgroundManager = default!;

    [CommandImplementation("Proto")]
    public void SetLobbyScreen(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] Prototype<AnimatedLobbyScreenPrototype> prototype)
    {
        _lobbyBackgroundManager.SetBackground(prototype.Value);
    }

    [CommandImplementation("File")]
    public void SetLobbyScreen(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] ResPath resPath)
    {
        _lobbyBackgroundManager.SetBackground(new ResPath("/Uploaded" + resPath));
    }
}


