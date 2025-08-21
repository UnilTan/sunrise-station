using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Enhanced memory component that stores signal values with locking capability.
/// Based on the "Память" component from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(EnhancedMemorySystem))]
public sealed partial class EnhancedMemoryComponent : Component
{
    /// <summary>
    /// Name of the input port for data.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "MemoryInput";

    /// <summary>
    /// Name of the port for controlling the lock state.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> LockStatePort = "LockState";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    /// <summary>
    /// The stored value. Cannot be empty.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string StoredValue = "";

    /// <summary>
    /// Whether input signals are currently being accepted.
    /// "1" = accept signals, anything else = ignore signals, empty = don't change
    /// </summary>
    [DataField]
    public bool AcceptingInput = true;

    /// <summary>
    /// Last output signal to track changes.
    /// </summary>
    [DataField]
    public LogicSignalData? LastOutput;
}