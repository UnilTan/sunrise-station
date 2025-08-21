logic-gate-examine = Сейчас установлена логическая операция { $gate }.
logic-gate-cycle = Переключено на операцию { $gate }

# Усовершенствованные логические компоненты
logic-not-examine = Текущий режим: {$mode}
logic-not-treat-empty-as-false = Пустые сигналы как ложь
logic-not-treat-empty-as-null = Пустые сигналы как пусто
logic-not-mode-changed = Переключен режим: {$mode}

enhanced-memory-examine = Сохранённое значение: "{$value}", Статус: {$status}
enhanced-memory-accepting = принимает ввод
enhanced-memory-locked = заблокирован
enhanced-memory-manual-edit = Используйте отвёртку для редактирования сохранённого значения

# Компонент задержки
delay-gate-examine = Задержка: {$delay}с, Сброс при сигнале: {$reset-signal}, Сброс при изменении: {$reset-change}
delay-gate-mode-changed = Переключён режим: {$mode}
delay-gate-mode-normal = Обычная задержка
delay-gate-mode-reset-signal = Сброс при сигнале (импульс)
delay-gate-mode-reset-change = Сброс при изменении (сглаживание)
delay-gate-mode-both = Оба режима сброса

# Арифметический компонент
arithmetic-gate-examine = Текущая операция: {$operation}
arithmetic-gate-operation-changed = Переключена операция: {$operation}
arithmetic-operation-add = Сложение
arithmetic-operation-subtract = Вычитание
arithmetic-operation-multiply = Умножение
arithmetic-operation-divide = Деление
arithmetic-operation-sin = Синус
arithmetic-operation-cos = Косинус
arithmetic-operation-sqrt = Квадратный корень
arithmetic-operation-abs = Абсолютное значение
arithmetic-operation-floor = Округление вниз
arithmetic-operation-ceil = Округление вверх

# WiFi компонент
wifi-gate-examine = Режим: {$mode}, Канал: {$channel}, Цель: "{$target}"
wifi-gate-receiving = приём
wifi-gate-transmitting = передача
wifi-gate-channel-changed = Переключён канал: {$channel}

# Силовое реле
power-relay-examine = Статус: {$state}, Макс. мощность: {$max-power}Вт
power-relay-active = активно
power-relay-inactive = неактивно
power-relay-toggle = Реле переключено в: {$state}

power-sensor-examine =
    Сейчас проверяется  { $output ->
        [true] выход
       *[false] вход
    } сети.
power-sensor-voltage-examine = Проверяется сеть с напряжением { $voltage }.
power-sensor-switch =
    Переключено на проверку { $output ->
        [true] выход
       *[false] вход
    } сети.
power-sensor-voltage-switch = Сеть переключена на { $voltage }!
