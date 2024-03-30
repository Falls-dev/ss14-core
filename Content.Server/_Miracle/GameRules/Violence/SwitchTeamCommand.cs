using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;
using Robust.Shared.Network;

// This was written with copilot, beware.
namespace Content.Server._Miracle.GameRules
{
    class SwitchTeamCommand : IConsoleCommand
    {
        public string Command => "switchteam";
        public string Description => "Switches the player's team.";
        public string Help => "switchteam <playerId> <newTeamId>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Expected exactly 2 arguments.");
                return;
            }

            if (!Guid.TryParse(args[0], out var guid))
            {
                shell.WriteLine($"Invalid player ID: {args[0]}");
                return;
            }

            var playerId = new NetUserId(guid);

            if (!ushort.TryParse(args[1], out var newTeamId))
            {
                shell.WriteLine($"Invalid team ID: {args[1]}");
                return;
            }

            var violenceRuleSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ViolenceRuleSystem>();
            violenceRuleSystem.SwitchTeam(playerId, newTeamId);
        }
    }
}
