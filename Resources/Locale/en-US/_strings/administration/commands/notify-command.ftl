cmd-notify-desc = Send a notification popup to players.
cmd-notify-help = {$command} <all|username1,username2,...> <title> [message] - Send notification popup. If message is not provided, you will be prompted to enter it.

# Usage examples in help text
cmd-notify-example-all = notify all "Important" "Server will restart in 5 minutes"
cmd-notify-example-players = notify player1,player2 "Alert" "You have been selected for an event"
cmd-notify-example-prompt = notify all "Notice"  # Will prompt for message

# Completion hints
cmd-notify-arg-target = <all|username1,username2,...>
cmd-notify-arg-title = <title>
cmd-notify-arg-message = [message]

# Command feedback messages
cmd-notify-sent-to-all = Notification sent to all {$count} players.
cmd-notify-sent-to-players = Notification sent to {$sent} player(s).
cmd-notify-players-not-found = Could not find players: {$players}
cmd-notify-cancelled = Notification cancelled.
cmd-notify-message-prompt-title = Enter Notification Message
cmd-notify-message-prompt = Message: