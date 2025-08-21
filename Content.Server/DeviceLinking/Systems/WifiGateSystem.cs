using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling WiFi logic gates.
/// Implements the "WiFi" component behavior from the issue description.
/// </summary>
public sealed class WifiGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public const string WifiSignalDataKey = "wifi_signal_data";
    public const string WifiChannelKey = "wifi_channel";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WifiGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WifiGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<WifiGateComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<WifiGateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WifiGateComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInit(EntityUid uid, WifiGateComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort, comp.SetTargetPort);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);

        comp.LastSignal = LogicSignalData.Empty();
        comp.LastOutput = LogicSignalData.Empty();
    }

    private void OnExamined(EntityUid uid, WifiGateComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var mode = comp.IsReceiving ? "receiving" : "transmitting";
        args.PushMarkup(Loc.GetString("wifi-gate-examine", 
            ("mode", Loc.GetString($"wifi-gate-{mode}")),
            ("channel", comp.Channel),
            ("target", comp.TargetSignal)));
    }

    private void OnInteractUsing(EntityUid uid, WifiGateComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, "Screwing"))
            return;

        // Cycle channel (1-10)
        comp.Channel = (comp.Channel % 10) + 1;

        _popup.PopupEntity(Loc.GetString("wifi-gate-channel-changed", ("channel", comp.Channel)), uid, args.User);
        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, WifiGateComponent comp, ref SignalReceivedEvent args)
    {
        var signal = LogicSignalData.FromNetworkPayload(args.Data);

        if (args.Port == comp.SetTargetPort)
        {
            // Update target signal
            if (!signal.IsEmpty)
            {
                comp.TargetSignal = signal.GetStringValue();
            }
            return;
        }

        if (args.Port == comp.InputPort)
        {
            comp.LastSignal = signal;

            if (!signal.IsEmpty)
            {
                // Switch to transmitting mode and broadcast signal
                comp.IsReceiving = false;
                TransmitWirelessSignal(uid, comp, signal);
                
                // Stop receiving mode
                ClearOutput(uid, comp);
            }
            else
            {
                // Return to receiving mode when signal is empty
                comp.IsReceiving = true;
            }
        }
    }

    private void OnPacketReceived(EntityUid uid, WifiGateComponent comp, DeviceNetworkPacketEvent args)
    {
        // Only process packets when in receiving mode
        if (!comp.IsReceiving)
            return;

        // Check if packet is on our channel
        if (!args.Data.TryGetValue(WifiChannelKey, out int channel) || channel != comp.Channel)
            return;

        // Get signal data from packet
        if (!args.Data.TryGetValue(WifiSignalDataKey, out string? signalData))
            return;

        var receivedSignal = LogicSignalData.String(signalData);
        ProcessReceivedSignal(uid, comp, receivedSignal);
    }

    private void TransmitWirelessSignal(EntityUid uid, WifiGateComponent comp, LogicSignalData signal)
    {
        if (!TryComp<DeviceNetworkComponent>(uid, out var network))
            return;

        var payload = new NetworkPayload
        {
            [WifiChannelKey] = comp.Channel,
            [WifiSignalDataKey] = signal.GetStringValue()
        };

        // Broadcast to all WiFi components in range on the same channel
        _deviceNetwork.QueuePacket(uid, null, payload, network.TransmitFrequency);
    }

    private void ProcessReceivedSignal(EntityUid uid, WifiGateComponent comp, LogicSignalData signal)
    {
        // Compare with target signal
        var targetSignal = LogicSignalData.String(comp.TargetSignal);
        bool matches = signal.Equals(targetSignal);

        LogicSignalData output;
        if (matches)
        {
            output = string.IsNullOrEmpty(comp.SuccessOutput) ? 
                LogicSignalData.Empty() : 
                LogicSignalData.String(comp.SuccessOutput);
        }
        else
        {
            output = string.IsNullOrEmpty(comp.FailureOutput) ? 
                LogicSignalData.Empty() : 
                LogicSignalData.String(comp.FailureOutput);
        }

        // Only send if output changed
        if (comp.LastOutput == null || !comp.LastOutput.Equals(output))
        {
            comp.LastOutput = output;

            if (output.IsEmpty)
                _deviceLink.SendEmptySignal(uid, comp.OutputPort);
            else
                _deviceLink.SendSignal(uid, comp.OutputPort, output);
        }
    }

    private void ClearOutput(EntityUid uid, WifiGateComponent comp)
    {
        comp.LastOutput = LogicSignalData.Empty();
        _deviceLink.SendEmptySignal(uid, comp.OutputPort);
    }
}