using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Logic component that compares two signals and outputs success or failure.
/// Based on the "Равно" component from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(EqualsGateSystem))]
public sealed partial class EqualsGateComponent : Component
{
    /// <summary>
    /// Name of the first input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPortA = "InputA";

    /// <summary>
    /// Name of the second input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPortB = "InputB";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    /// <summary>
    /// Value to output when comparison is successful (equal).
    /// Can be a number or string.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string SuccessOutput = "1";

    /// <summary>
    /// Value to output when comparison fails (not equal).
    /// Empty string means empty signal.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string FailureOutput = "";

    // State tracking for the memory behavior described in the issue
    [DataField]
    public LogicSignalData? LastSignalA;

    [DataField]
    public LogicSignalData? LastSignalB;

    [DataField]
    public LogicSignalData? LastOutput;
}