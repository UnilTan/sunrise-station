using Content.Client.UserInterface.Systems.MentorHelp;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class OpenMentorHelpCommand : LocalizedCommands
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override string Command => "openmhelp";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length >= 1)
        {
            shell.WriteLine(Help);
            return;
        }

        _userInterfaceManager.GetUIController<MentorHelpUIController>().Open();
    }
}