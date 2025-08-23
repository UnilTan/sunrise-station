cmd-admin-notify-desc = Отправить всплывающее уведомление игрокам.
cmd-admin-notify-help = {$command} <all|username1,username2,...> <заголовок> [сообщение] - Отправить всплывающее уведомление. Если сообщение не указано, будет запрошен ввод.

# Примеры использования в тексте справки
cmd-admin-notify-example-all = notify all "Важно" "Сервер будет перезапущен через 5 минут"
cmd-admin-notify-example-players = notify player1,player2 "Оповещение" "Вы были выбраны для события"
cmd-admin-notify-example-prompt = notify all "Уведомление"  # Запросит ввод сообщения

# Подсказки для автодополнения
cmd-admin-notify-arg-target = <all|username1,username2,...>
cmd-admin-notify-arg-title = <заголовок>
cmd-admin-notify-arg-message = [сообщение]

# Сообщения обратной связи команды
cmd-admin-notify-sent-to-all = Уведомление отправлено всем {$count} игрокам.
cmd-admin-notify-sent-to-players = Уведомление отправлено {$sent} игроку(ам).
cmd-admin-notify-players-not-found = Не удалось найти игроков: {$players}
cmd-admin-notify-cancelled = Уведомление отменено.
cmd-admin-notify-message-prompt-title = Введите текст уведомления
cmd-admin-notify-message-prompt = Сообщение: