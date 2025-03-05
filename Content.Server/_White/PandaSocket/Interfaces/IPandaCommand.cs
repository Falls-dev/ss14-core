using Content.Server._White.PandaSocket.Main;

namespace Content.Server._White.PandaSocket.Interfaces;

public interface IPandaCommand
{
    public string Name { get; }
    public Type RequestMessageType { get; }
    public void Execute(IPandaStatusHandlerContext context, PandaBaseMessage baseMessage);
    public void Response(IPandaStatusHandlerContext context, PandaBaseMessage? message = null);
}
