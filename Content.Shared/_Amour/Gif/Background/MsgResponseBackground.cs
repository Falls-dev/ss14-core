using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Amour.Gif.Background;

public sealed class MsgResponseBackground : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public ResPath Path { get; set; }
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Path = new ResPath(buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Path.ToString());
        buffer.Write(ResPath.Separator);
    }
}
