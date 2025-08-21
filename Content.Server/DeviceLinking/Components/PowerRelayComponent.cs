using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Logic component that controls both power flow and signal routing.
/// Based on the "Реле" component from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(PowerRelaySystem))]
public sealed partial class PowerRelayComponent : Component
{
    /// <summary>
    /// Port for toggling the relay state.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// Port for setting the relay state directly.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> SetStatePort = "SetState";

    /// <summary>
    /// First signal input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> SignalInputA = "SignalInputA";

    /// <summary>
    /// Second signal input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> SignalInputB = "SignalInputB";

    /// <summary>
    /// Port that outputs the current relay state.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> StateOutputPort = "StateOutput";

    /// <summary>
    /// First signal output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> SignalOutputA = "SignalOutputA";

    /// <summary>
    /// Second signal output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> SignalOutputB = "SignalOutputB";

    /// <summary>
    /// Port that outputs the power load demand.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LoadOutputPort = "LoadOutput";

    /// <summary>
    /// Port that outputs the actual power being supplied.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> PowerOutputPort = "PowerOutput";

    /// <summary>
    /// Maximum power that can flow through this relay (in watts).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxPowerFlow = 1000.0f;

    /// <summary>
    /// Whether the relay is currently active (passing power and signals).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive = false;

    /// <summary>
    /// Last received signals for routing.
    /// </summary>
    [DataField]
    public LogicSignalData? LastSignalA;

    [DataField]
    public LogicSignalData? LastSignalB;

    /// <summary>
    /// Last outputs to track changes.
    /// </summary>
    [DataField]
    public LogicSignalData? LastStateOutput;

    [DataField]
    public LogicSignalData? LastSignalOutputA;

    [DataField]
    public LogicSignalData? LastSignalOutputB;

    [DataField]
    public LogicSignalData? LastLoadOutput;

    [DataField]
    public LogicSignalData? LastPowerOutput;
}