using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling arithmetic logic gates.
/// Implements arithmetic components from the issue description.
/// </summary>
public sealed class ArithmeticGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    private readonly int OperationCount = Enum.GetValues(typeof(ArithmeticOperation)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArithmeticGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ArithmeticGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ArithmeticGateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ArithmeticGateComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInit(EntityUid uid, ArithmeticGateComponent comp, ComponentInit args)
    {
        // Always ensure InputA, only ensure InputB for binary operations
        _deviceLink.EnsureSinkPorts(uid, comp.InputPortA);
        if (!comp.IsUnaryOperation)
            _deviceLink.EnsureSinkPorts(uid, comp.InputPortB);
        
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);

        comp.LastSignalA = LogicSignalData.Empty();
        comp.LastSignalB = LogicSignalData.Empty();
        comp.LastOutput = LogicSignalData.Empty();
    }

    private void OnExamined(EntityUid uid, ArithmeticGateComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var opName = Loc.GetString($"arithmetic-operation-{comp.Operation.ToString().ToLower()}");
        args.PushMarkup(Loc.GetString("arithmetic-gate-examine", ("operation", opName)));
    }

    private void OnInteractUsing(EntityUid uid, ArithmeticGateComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, "Screwing"))
            return;

        // Cycle through operations
        var operation = (int)comp.Operation;
        operation = (operation + 1) % OperationCount;
        comp.Operation = (ArithmeticOperation)operation;

        // Update ports if operation type changed
        UpdatePorts(uid, comp);

        var opName = Loc.GetString($"arithmetic-operation-{comp.Operation.ToString().ToLower()}");
        _popup.PopupEntity(Loc.GetString("arithmetic-gate-operation-changed", ("operation", opName)), uid, args.User);

        // Recalculate output
        UpdateOutput(uid, comp);
        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, ArithmeticGateComponent comp, ref SignalReceivedEvent args)
    {
        var signal = LogicSignalData.FromNetworkPayload(args.Data);

        if (args.Port == comp.InputPortA)
        {
            comp.LastSignalA = signal;
        }
        else if (args.Port == comp.InputPortB && !comp.IsUnaryOperation)
        {
            comp.LastSignalB = signal;
        }
        else
        {
            return; // Unknown port
        }

        UpdateOutput(uid, comp);
    }

    private void UpdatePorts(EntityUid uid, ArithmeticGateComponent comp)
    {
        if (comp.IsUnaryOperation)
        {
            // Remove InputB port if it exists
            if (TryComp<DeviceLinkSinkComponent>(uid, out var sink))
            {
                sink.Ports.Remove(comp.InputPortB);
            }
        }
        else
        {
            // Ensure InputB port exists
            _deviceLink.EnsureSinkPorts(uid, comp.InputPortB);
        }
    }

    private void UpdateOutput(EntityUid uid, ArithmeticGateComponent comp)
    {
        var signalA = comp.LastSignalA ?? LogicSignalData.Empty();
        var signalB = comp.LastSignalB ?? LogicSignalData.Empty();

        LogicSignalData output;

        // Check for empty signals
        if (signalA.IsEmpty || (!comp.IsUnaryOperation && signalB.IsEmpty))
        {
            output = LogicSignalData.Empty();
        }
        else
        {
            var valueA = signalA.GetNumericValue();
            var valueB = comp.IsUnaryOperation ? 0.0f : signalB.GetNumericValue();

            float result = comp.Operation switch
            {
                ArithmeticOperation.Add => valueA + valueB,
                ArithmeticOperation.Subtract => valueA - valueB,
                ArithmeticOperation.Multiply => valueA * valueB,
                ArithmeticOperation.Divide => HandleDivision(valueA, valueB),
                ArithmeticOperation.Sin => MathF.Sin(valueA * MathF.PI / 180), // Convert degrees to radians
                ArithmeticOperation.Cos => MathF.Cos(valueA * MathF.PI / 180),
                ArithmeticOperation.Sqrt => valueA >= 0 ? MathF.Sqrt(valueA) : float.NaN,
                ArithmeticOperation.Abs => MathF.Abs(valueA),
                ArithmeticOperation.Floor => MathF.Floor(valueA),
                ArithmeticOperation.Ceil => MathF.Ceiling(valueA),
                _ => 0.0f
            };

            // Check for invalid results
            if (float.IsNaN(result) || float.IsInfinity(result))
            {
                output = LogicSignalData.Empty();
            }
            else
            {
                // Apply limits
                result = Math.Clamp(result, comp.MinValue, comp.MaxValue);
                output = LogicSignalData.Numeric(result);
            }
        }

        // Only send signal if output actually changed
        if (comp.LastOutput == null || !comp.LastOutput.Equals(output))
        {
            comp.LastOutput = output;

            if (output.IsEmpty)
                _deviceLink.SendEmptySignal(uid, comp.OutputPort);
            else
                _deviceLink.SendSignal(uid, comp.OutputPort, output);
        }
    }

    private float HandleDivision(float a, float b)
    {
        // Handle division by zero and very small numbers as described in the issue
        if (MathF.Abs(b) < 0.0001f)
            return float.NaN; // Will be converted to empty signal
        
        return a / b;
    }
}