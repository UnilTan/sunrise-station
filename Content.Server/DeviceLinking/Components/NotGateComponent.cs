using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Logic component that inverts signals.
/// Based on the "Не" component from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(NotGateSystem))]
public sealed partial class NotGateComponent : Component
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
    /// Whether to treat empty signals as false (when false) or true (when true).
    /// This corresponds to the "Тип сигнала" checkbox in the issue.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool TreatEmptyAsFalse = false;

    /// <summary>
    /// Last received input signal.
    /// </summary>
    [DataField]
    public LogicSignalData? LastInput;

    /// <summary>
    /// Last sent output signal.
    /// </summary>
    [DataField]
    public LogicSignalData? LastOutput;
}