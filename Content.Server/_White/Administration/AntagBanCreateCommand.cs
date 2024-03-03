using Robust.Shared.Console;

namespace Content.Server._White.Administration;

public sealed class AntagBanCreateCommand : IConsoleCommand
{
    public string Command => "antagban";
    public string Description => "Bans player antag role.";
    public string Help => "antagban <ckey> <role> <reason> <time> <isglobal>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        return;
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), "CKEY");
    }
}
