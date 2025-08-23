using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.Administration;

public sealed partial class QuickDialogSystem
{
    /// <summary>
    /// Opens a simple notification dialog for the given client.
    /// Shows only a message with an OK button.
    /// </summary>
    /// <param name="session">Client to show the notification to.</param>
    /// <param name="title">Title of the notification dialog.</param>
    /// <param name="message">The message to display to the user.</param>
    /// <param name="onClosed">Optional action to execute when the dialog is closed.</param>
    public void OpenNotification(ICommonSession session, string title, string message, Action? onClosed = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>(), // No input fields for simple notification
            QuickDialogButtonFlag.OkButton, // Only OK button
            (_ => onClosed?.Invoke()), // OK button action
            onClosed ?? (() => { }), // Cancel/close action
            message // Pass the message to display
        );
    }

    /// <summary>
    /// Sends a notification to all connected players.
    /// </summary>
    /// <param name="title">Title of the notification dialog.</param>
    /// <param name="message">The message to display to all users.</param>
    public void NotifyAllPlayers(string title, string message)
    {
        foreach (var session in _playerManager.Sessions)
        {
            OpenNotification(session, title, message);
        }
    }

    /// <summary>
    /// Sends a notification to specific players by username.
    /// </summary>
    /// <param name="title">Title of the notification dialog.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="usernames">List of usernames to send the notification to.</param>
    /// <returns>List of usernames that couldn't be found.</returns>
    public List<string> NotifyPlayers(string title, string message, params string[] usernames)
    {
        var notFound = new List<string>();
        
        foreach (var username in usernames)
        {
            var session = _playerManager.Sessions.FirstOrDefault(s => 
                s.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (session == null)
            {
                notFound.Add(username);
                continue;
            }
            
            OpenNotification(session, title, message);
        }
        
        return notFound;
    }
}