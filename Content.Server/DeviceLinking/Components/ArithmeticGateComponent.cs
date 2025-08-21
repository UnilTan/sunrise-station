using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Types of arithmetic operations.
/// </summary>
public enum ArithmeticOperation : byte
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Sin,
    Cos,
    Sqrt,
    Abs,
    Floor,
    Ceil
}

/// <summary>
/// Logic component that performs arithmetic operations on signals.
/// Based on the arithmetic components from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(ArithmeticGateSystem))]
public sealed partial class ArithmeticGateComponent : Component
{
    /// <summary>
    /// Name of the first input port (A).
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPortA = "InputA";

    /// <summary>
    /// Name of the second input port (B).
    /// Only used for binary operations.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPortB = "InputB";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    /// <summary>
    /// The arithmetic operation to perform.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ArithmeticOperation Operation = ArithmeticOperation.Add;

    /// <summary>
    /// Maximum output value.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxValue = 999999.0f;

    /// <summary>
    /// Minimum output value.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinValue = -999999.0f;

    /// <summary>
    /// Whether this is a unary operation (only uses input A).
    /// </summary>
    public bool IsUnaryOperation => Operation is ArithmeticOperation.Sin or ArithmeticOperation.Cos 
                                                  or ArithmeticOperation.Sqrt or ArithmeticOperation.Abs 
                                                  or ArithmeticOperation.Floor or ArithmeticOperation.Ceil;

    // State
    [DataField]
    public LogicSignalData? LastSignalA;

    [DataField]
    public LogicSignalData? LastSignalB;

    [DataField]
    public LogicSignalData? LastOutput;
}