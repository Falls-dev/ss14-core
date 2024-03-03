using Content.Server.Administration.Managers;
using Robust.Shared.Network;

namespace Content.Server._White.Administration;
// Antag Bans don't exist. (c) metalgearsloth - 01.02.2024
public sealed class AntagRoleBanSystem : EntitySystem
{
    [Dependency] private readonly BanManager _roleBanManager = default!;
    public override void Initialize()
    {

    }

    public bool IsAntagBanned(NetUserId nuid)
    {
        return false;
    }

    public bool CreateAntagBan(NetUserId nuid) // need more args
    {
        return false;
        //return _roleBanManager.CreateRoleBan(nuid);
    }

    public bool CreateAntagUnban(NetUserId nuid)
    {
        return false;
    }
}
