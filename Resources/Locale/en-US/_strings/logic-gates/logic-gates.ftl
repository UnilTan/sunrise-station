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

# Delay Gate
delay-gate-examine = Delay: {$delay}s, Reset on signal: {$reset-signal}, Reset on change: {$reset-change}
delay-gate-mode-changed = Switched to mode: {$mode}
delay-gate-mode-normal = Normal delay
delay-gate-mode-reset-signal = Reset on signal (impulse)
delay-gate-mode-reset-change = Reset on change (smoothing)
delay-gate-mode-both = Both reset modes

# Arithmetic Gate
arithmetic-gate-examine = Current operation: {$operation}
arithmetic-gate-operation-changed = Switched to operation: {$operation}
arithmetic-operation-add = Addition
arithmetic-operation-subtract = Subtraction
arithmetic-operation-multiply = Multiplication
arithmetic-operation-divide = Division
arithmetic-operation-sin = Sine
arithmetic-operation-cos = Cosine
arithmetic-operation-sqrt = Square Root
arithmetic-operation-abs = Absolute Value
arithmetic-operation-floor = Floor
arithmetic-operation-ceil = Ceiling

# WiFi Gate
wifi-gate-examine = Mode: {$mode}, Channel: {$channel}, Target: "{$target}"
wifi-gate-receiving = receiving
wifi-gate-transmitting = transmitting
wifi-gate-channel-changed = Switched to channel: {$channel}

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
