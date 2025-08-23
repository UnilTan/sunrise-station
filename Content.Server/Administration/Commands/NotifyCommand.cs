using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Server.Player;
using System.Linq;
using Robust.Shared.GameObjects;

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
            shell.WriteLine("Usage: notify <all|username1,username2,...> <title> [message]");
            shell.WriteLine("Examples:");
            shell.WriteLine("  notify all \"Important\" \"Server will restart in 5 minutes\"");
            shell.WriteLine("  notify player1,player2 \"Alert\" \"You have been selected for an event\"");
            shell.WriteLine("  notify all \"Notice\"  # Will prompt for message");
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
                shell.WriteError("Cannot prompt for message when running from server console. Please provide the message as an argument.");
                return;
            }

            quickDialog.OpenDialog<string>(shell.Player, "Enter Notification Message", "Message:", 
                (msg) => SendNotifications(shell, quickDialog, target, title, msg),
                () => shell.WriteLine("Notification cancelled."));
        }
    }

    private void SendNotifications(IConsoleShell shell, QuickDialogSystem quickDialog, string target, string title, string message)
    {
        if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // Send to all players
            quickDialog.NotifyAllPlayers(title, message);
            var playerCount = _playerManager.PlayerCount;
            shell.WriteLine($"Notification sent to all {playerCount} players.");
        }
        else
        {
            // Send to specific players
            var usernames = target.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(u => u.Trim())
                                  .ToArray();

            var notFound = quickDialog.NotifyPlayers(title, message, usernames);
            
            var sentCount = usernames.Length - notFound.Count;
            shell.WriteLine($"Notification sent to {sentCount} player(s).");
            
            if (notFound.Count > 0)
            {
                shell.WriteError($"Could not find players: {string.Join(", ", notFound)}");
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new List<string> { "all" };
            options.AddRange(_playerManager.Sessions.Select(s => s.Name));
            return CompletionResult.FromHintOptions(options, "Target (all or player names separated by commas)");
        }
        
        if (args.Length == 2)
        {
            return CompletionResult.FromHint("Title of the notification");
        }
        
        if (args.Length == 3)
        {
            return CompletionResult.FromHint("Message (optional - will prompt if not provided)");
        }

        return CompletionResult.Empty;
    }
}