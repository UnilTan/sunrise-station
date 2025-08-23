using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Server.Player;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class NotifyCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "notify";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var quickDialog = _entitySystemManager.GetEntitySystem<QuickDialogSystem>();

        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("cmd-notify-help", ("command", Command)));
            shell.WriteLine("");
            shell.WriteLine("Examples:");
            shell.WriteLine("  " + Loc.GetString("cmd-notify-example-all"));
            shell.WriteLine("  " + Loc.GetString("cmd-notify-example-players"));
            shell.WriteLine("  " + Loc.GetString("cmd-notify-example-prompt"));
            return;
        }

        var target = args[0];
        var title = args[1];
        string message;

        if (args.Length >= 3)
        {
            // Message provided as argument
            message = string.Join(" ", args[2..]);
            SendNotifications(shell, quickDialog, target, title, message);
        }
        else
        {
            // Prompt for message using dialog
            if (shell.Player == null)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            quickDialog.OpenDialog<string>(shell.Player, 
                Loc.GetString("cmd-notify-message-prompt-title"), 
                Loc.GetString("cmd-notify-message-prompt"), 
                (msg) => SendNotifications(shell, quickDialog, target, title, msg),
                () => shell.WriteLine(Loc.GetString("cmd-notify-cancelled")));
        }
    }

    private void SendNotifications(IConsoleShell shell, QuickDialogSystem quickDialog, string target, string title, string message)
    {
        if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // Send to all players
            quickDialog.NotifyAllPlayers(title, message);
            var playerCount = _playerManager.PlayerCount;
            shell.WriteLine(Loc.GetString("cmd-notify-sent-to-all", ("count", playerCount)));
        }
        else
        {
            // Send to specific players
            var usernames = target.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(u => u.Trim())
                                  .ToArray();

            var notFound = quickDialog.NotifyPlayers(title, message, usernames);
            
            var sentCount = usernames.Length - notFound.Count;
            shell.WriteLine(Loc.GetString("cmd-notify-sent-to-players", ("sent", sentCount)));
            
            if (notFound.Count > 0)
            {
                shell.WriteError(Loc.GetString("cmd-notify-players-not-found", ("players", string.Join(", ", notFound))));
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new List<string> { "all" };
            options.AddRange(_playerManager.Sessions.Select(s => s.Name));
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-notify-arg-target"));
        }
        
        if (args.Length == 2)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-notify-arg-title"));
        }
        
        if (args.Length == 3)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-notify-arg-message"));
        }

        return CompletionResult.Empty;
    }
}