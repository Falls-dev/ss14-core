using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Network;

// TODO: test permissions
namespace Content.Server._Miracle.GameRules.Violence;

[AdminCommand(AdminFlags.Admin)]
internal class SwitchTeamCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerLocator _locator = default!;

    public string Command => "switchteam";
    public string Description => "Switches the player's team.";
    public string Help => "switchteam <playerId> <newTeamId>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine("Expected exactly 2 arguments.");
            return;
        }

        var target = args[0];
        var located = await _locator.LookupIdByNameOrIdAsync(target);
        var player = shell.Player;

        if (player == null)
        {
            shell.WriteLine("Player not found.");
            return;
        }

        if (!ushort.TryParse(args[1], out var newTeamId))
        {
            shell.WriteLine($"Invalid team ID: {args[1]}");
            return;
        }

        var violenceRuleSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ViolenceRuleSystem>();
        violenceRuleSystem.SwitchTeam(player.UserId, newTeamId);
    }
}

