cmd-admin-notify-desc = Send a notification popup to players.
cmd-admin-notify-help = {$command} <all|username1,username2,...> <title> [message] - Send notification popup. If message is not provided, you will be prompted to enter it.

# Usage examples in help text
cmd-admin-notify-example-all = notify all "Important" "Server will restart in 5 minutes"
cmd-admin-notify-example-players = notify player1,player2 "Alert" "You have been selected for an event"
cmd-admin-notify-example-prompt = notify all "Notice"  # Will prompt for message

# Completion hints
cmd-admin-notify-arg-target = <all|username1,username2,...>
cmd-admin-notify-arg-title = <title>
cmd-admin-notify-arg-message = [message]

# Command feedback messages
cmd-admin-notify-sent-to-all = Notification sent to all {$count} players.
cmd-admin-notify-sent-to-players = Notification sent to {$sent} player(s).
cmd-admin-notify-players-not-found = Could not find players: {$players}
cmd-admin-notify-cancelled = Notification cancelled.
cmd-admin-notify-message-prompt-title = Enter Notification Message
cmd-admin-notify-message-prompt = Message: