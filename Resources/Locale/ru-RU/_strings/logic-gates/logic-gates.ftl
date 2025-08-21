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
