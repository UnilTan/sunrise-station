logic-gate-examine = It is currently {INDEFINITE($gate)} {$gate} gate.

logic-gate-cycle = Switched to {INDEFINITE($gate)} {$gate} gate

# Enhanced Logic Components
logic-not-examine = Current mode: {$mode}
logic-not-treat-empty-as-false = Treat empty signals as false
logic-not-treat-empty-as-null = Treat empty signals as null  
logic-not-mode-changed = Switched to mode: {$mode}

enhanced-memory-examine = Stored value: "{$value}", Status: {$status}
enhanced-memory-accepting = accepting input
enhanced-memory-locked = locked
enhanced-memory-manual-edit = Use a screwdriver to edit the stored value

power-sensor-examine = It is currently checking the network's {$output ->
    [true] output
    *[false] input
} battery.
power-sensor-voltage-examine = It is checking the {$voltage} power network.

power-sensor-switch = Switched to checking the network's {$output ->
    [true] output
    *[false] input
} battery.
power-sensor-voltage-switch = Switched network to {$voltage}!
