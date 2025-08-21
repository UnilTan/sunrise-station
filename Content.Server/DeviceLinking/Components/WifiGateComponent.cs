using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Logic component that can transmit and receive wireless signals.
/// Based on the "WiFi" component from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(WifiGateSystem))]
public sealed partial class WifiGateComponent : Component
{
    /// <summary>
    /// Name of the input port for transmitting signals.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Input";

    /// <summary>
    /// Name of the port for setting the target signal.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> SetTargetPort = "SetTarget";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    /// <summary>
    /// The target signal to compare against.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string TargetSignal = "1";

    /// <summary>
    /// Success output when signal matches target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string SuccessOutput = "1";

    /// <summary>
    /// Failure output when signal doesn't match target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string FailureOutput = "0";

    /// <summary>
    /// Wireless transmission channel.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Channel = 1;

    /// <summary>
    /// Transmission range for wireless signals.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 200.0f;

    /// <summary>
    /// Whether this component is currently receiving (true) or transmitting (false).
    /// </summary>
    [DataField]
    public bool IsReceiving = true;

    /// <summary>
    /// Last received/transmitted signal for comparison.
    /// </summary>
    [DataField]
    public LogicSignalData? LastSignal;

    /// <summary>
    /// Last output signal.
    /// </summary>
    [DataField]
    public LogicSignalData? LastOutput;
}