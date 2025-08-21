using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling equals comparison logic gates.
/// Implements the "Равно" component behavior from the issue description.
/// </summary>
public sealed class EqualsGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EqualsGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EqualsGateComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, EqualsGateComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPortA, comp.InputPortB);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);

        // Initialize with empty signals
        comp.LastSignalA = LogicSignalData.Empty();
        comp.LastSignalB = LogicSignalData.Empty();
        comp.LastOutput = LogicSignalData.Empty();
    }

    private void OnSignalReceived(EntityUid uid, EqualsGateComponent comp, ref SignalReceivedEvent args)
    {
        var signal = LogicSignalData.FromNetworkPayload(args.Data);

        // Update the appropriate input
        if (args.Port == comp.InputPortA)
        {
            comp.LastSignalA = signal;
        }
        else if (args.Port == comp.InputPortB)
        {
            comp.LastSignalB = signal;
        }
        else
        {
            return; // Unknown port
        }

        UpdateOutput(uid, comp);
    }

    private void UpdateOutput(EntityUid uid, EqualsGateComponent comp)
    {
        // Implement the complex comparison logic described in the issue
        var signalA = comp.LastSignalA ?? LogicSignalData.Empty();
        var signalB = comp.LastSignalB ?? LogicSignalData.Empty();

        LogicSignalData output;

        // Special case: if both signals are empty, output empty regardless of success/failure settings
        if (signalA.IsEmpty && signalB.IsEmpty)
        {
            output = LogicSignalData.Empty();
        }
        else
        {
            // Compare the signals
            bool areEqual = signalA.Equals(signalB);

            if (areEqual)
            {
                // Output success value
                if (string.IsNullOrEmpty(comp.SuccessOutput))
                    output = LogicSignalData.Empty();
                else
                    output = LogicSignalData.String(comp.SuccessOutput);
            }
            else
            {
                // Output failure value
                if (string.IsNullOrEmpty(comp.FailureOutput))
                    output = LogicSignalData.Empty();
                else
                    output = LogicSignalData.String(comp.FailureOutput);
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
}