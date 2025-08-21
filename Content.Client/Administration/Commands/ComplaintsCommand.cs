using Content.Client.Administration.UI.Complaint;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client.Administration.Commands
{
    [AnyCommand]
    public sealed class ComplaintsCommand : LocalizedCommands
    {
        public override string Command => "complaints";

        public override string Help => Loc.GetString($"cmd-{Command}-help");

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var window = new ComplaintListWindow();
            window.OpenCentered();
        }
    }
}