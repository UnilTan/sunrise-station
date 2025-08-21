using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Logic component that delays signals by a specified time.
/// Based on the "Задержка" component from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(DelayGateSystem))]
public sealed partial class DelayGateComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Input";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    /// <summary>
    /// Delay time in seconds.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DelayTime = 5.0f;

    /// <summary>
    /// Reset output on signal change (impulse mode).
    /// When enabled, output is empty during delay and pulses briefly with the last signal.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ResetOnSignal = false;

    /// <summary>
    /// Reset on different signal (smoothing mode).
    /// When enabled, output is empty during delay after signal changes.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ResetOnChange = false;

    /// <summary>
    /// Queue of delayed signals with their timestamps.
    /// </summary>
    [DataField]
    public Queue<DelayedSignal> SignalQueue = new();

    /// <summary>
    /// Current output signal.
    /// </summary>
    [DataField]
    public LogicSignalData? CurrentOutput;

    /// <summary>
    /// Last input signal for change detection.
    /// </summary>
    [DataField]
    public LogicSignalData? LastInput;

    /// <summary>
    /// Time when current delay started (for reset modes).
    /// </summary>
    [DataField]
    public TimeSpan? DelayStartTime;
}

/// <summary>
/// Represents a signal that is waiting to be output after a delay.
/// </summary>
[DataField]
public sealed partial class DelayedSignal
{
    /// <summary>
    /// The signal data to output.
    /// </summary>
    [DataField]
    public LogicSignalData Signal { get; set; } = LogicSignalData.Empty();

    /// <summary>
    /// When this signal should be output.
    /// </summary>
    [DataField]
    public TimeSpan OutputTime { get; set; }

    /// <summary>
    /// Duration of this signal (for maintaining timing).
    /// </summary>
    [DataField]
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0.017); // One tick

    public DelayedSignal()
    {
    }

    public DelayedSignal(LogicSignalData signal, TimeSpan outputTime, TimeSpan duration)
    {
        Signal = signal;
        OutputTime = outputTime;
        Duration = duration;
    }
}