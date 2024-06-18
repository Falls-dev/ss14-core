using Content.Client.GameTicking.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client.GameTicking.Commands;

[AnyCommand]
public sealed class ShowManifestCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    public string Command => "showmanifest";
    public string Description => "Shows manifest if you closed one";
    public string Help => "Usage: showmanifest";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ticker = _entitySystem.GetEntitySystem<ClientGameTicker>();
        var window = ticker._window;

        if (window == null)
        {
            shell.WriteLine("This can only be executed while the game is not in a round.");
            return;
        }

        window.OpenCentered();
    }
}
