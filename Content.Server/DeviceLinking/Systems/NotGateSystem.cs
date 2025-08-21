using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling NOT logic gates.
/// Implements the "Не" component behavior from the issue description.
/// </summary>
public sealed class NotGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NotGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NotGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<NotGateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NotGateComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInit(EntityUid uid, NotGateComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);

        // Initialize with empty signals
        comp.LastInput = LogicSignalData.Empty();
        comp.LastOutput = LogicSignalData.Empty();
    }

    private void OnExamined(EntityUid uid, NotGateComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var mode = comp.TreatEmptyAsFalse ? "logic-not-treat-empty-as-false" : "logic-not-treat-empty-as-null";
        args.PushMarkup(Loc.GetString("logic-not-examine", ("mode", Loc.GetString(mode))));
    }

    private void OnInteractUsing(EntityUid uid, NotGateComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, "Screwing"))
            return;

        // Toggle the mode
        comp.TreatEmptyAsFalse = !comp.TreatEmptyAsFalse;

        var mode = comp.TreatEmptyAsFalse ? "logic-not-treat-empty-as-false" : "logic-not-treat-empty-as-null";
        var msg = Loc.GetString("logic-not-mode-changed", ("mode", Loc.GetString(mode)));
        _popup.PopupEntity(msg, uid, args.User);

        // Update output based on new mode
        UpdateOutput(uid, comp);
        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, NotGateComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.InputPort)
            return;

        var signal = LogicSignalData.FromNetworkPayload(args.Data);
        comp.LastInput = signal;

        UpdateOutput(uid, comp);
    }

    private void UpdateOutput(EntityUid uid, NotGateComponent comp)
    {
        var input = comp.LastInput ?? LogicSignalData.Empty();
        LogicSignalData output;

        if (input.IsEmpty)
        {
            if (comp.TreatEmptyAsFalse)
            {
                // Empty treated as false -> output true (1)
                output = LogicSignalData.String("1");
            }
            else
            {
                // Empty treated as empty -> output empty
                output = LogicSignalData.Empty();
            }
        }
        else
        {
            // Invert the truthiness of the signal
            bool inputTruthy = input.GetBooleanValue();
            output = LogicSignalData.String(inputTruthy ? "0" : "1");
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