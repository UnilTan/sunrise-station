using Content.Server.DeviceLinking.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling power relay logic components.
/// Implements the "Реле" component behavior from the issue description.
/// </summary>
public sealed class PowerRelaySystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly PowerNetSystem _powerNet = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerRelayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PowerRelayComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<PowerRelayComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PowerRelayComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Update power and signal outputs
        var query = EntityQueryEnumerator<PowerRelayComponent, DeviceLinkSourceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var source))
        {
            UpdateOutputs((uid, comp, source));
        }
    }

    private void OnInit(EntityUid uid, PowerRelayComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.TogglePort, comp.SetStatePort, comp.SignalInputA, comp.SignalInputB);
        _deviceLink.EnsureSourcePorts(uid, comp.StateOutputPort, comp.SignalOutputA, comp.SignalOutputB, 
                                     comp.LoadOutputPort, comp.PowerOutputPort);

        // Initialize signal states
        comp.LastSignalA = LogicSignalData.Empty();
        comp.LastSignalB = LogicSignalData.Empty();
        comp.LastStateOutput = LogicSignalData.Boolean(comp.IsActive);
        comp.LastSignalOutputA = LogicSignalData.Empty();
        comp.LastSignalOutputB = LogicSignalData.Empty();
        comp.LastLoadOutput = LogicSignalData.Numeric(0);
        comp.LastPowerOutput = LogicSignalData.Numeric(0);
    }

    private void OnExamined(EntityUid uid, PowerRelayComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var state = comp.IsActive ? "active" : "inactive";
        args.PushMarkup(Loc.GetString("power-relay-examine", 
            ("state", Loc.GetString($"power-relay-{state}")),
            ("max-power", comp.MaxPowerFlow)));
    }

    private void OnInteractUsing(EntityUid uid, PowerRelayComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, "Pulsing"))
            return;

        // Manual toggle with multitool
        comp.IsActive = !comp.IsActive;
        
        var state = comp.IsActive ? "active" : "inactive";
        _popup.PopupEntity(Loc.GetString("power-relay-toggle", 
            ("state", Loc.GetString($"power-relay-{state}"))), uid, args.User);
        
        UpdateOutputs((uid, comp, null));
        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, PowerRelayComponent comp, ref SignalReceivedEvent args)
    {
        var signal = LogicSignalData.FromNetworkPayload(args.Data);

        if (args.Port == comp.TogglePort)
        {
            // Toggle state on any non-empty signal that's not "0"
            if (!signal.IsEmpty && signal.GetStringValue() != "0")
            {
                comp.IsActive = !comp.IsActive;
            }
        }
        else if (args.Port == comp.SetStatePort)
        {
            // Set state directly
            if (!signal.IsEmpty)
            {
                comp.IsActive = signal.GetStringValue() == "1";
            }
        }
        else if (args.Port == comp.SignalInputA)
        {
            comp.LastSignalA = signal;
        }
        else if (args.Port == comp.SignalInputB)
        {
            comp.LastSignalB = signal;
        }

        UpdateOutputs((uid, comp, null));
    }

    private void UpdateOutputs(Entity<PowerRelayComponent, DeviceLinkSourceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        var (uid, comp, source) = ent;

        // Update state output
        var stateOutput = LogicSignalData.Boolean(comp.IsActive);
        if (comp.LastStateOutput == null || !comp.LastStateOutput.Equals(stateOutput))
        {
            comp.LastStateOutput = stateOutput;
            _deviceLink.SendSignal(uid, comp.StateOutputPort, stateOutput, source);
        }

        // Update signal outputs (only pass through if active)
        LogicSignalData signalOutputA = comp.IsActive ? 
            (comp.LastSignalA ?? LogicSignalData.Empty()) : 
            LogicSignalData.Empty();
        
        LogicSignalData signalOutputB = comp.IsActive ? 
            (comp.LastSignalB ?? LogicSignalData.Empty()) : 
            LogicSignalData.Empty();

        if (comp.LastSignalOutputA == null || !comp.LastSignalOutputA.Equals(signalOutputA))
        {
            comp.LastSignalOutputA = signalOutputA;
            if (signalOutputA.IsEmpty)
                _deviceLink.SendEmptySignal(uid, comp.SignalOutputA, source);
            else
                _deviceLink.SendSignal(uid, comp.SignalOutputA, signalOutputA, source);
        }

        if (comp.LastSignalOutputB == null || !comp.LastSignalOutputB.Equals(signalOutputB))
        {
            comp.LastSignalOutputB = signalOutputB;
            if (signalOutputB.IsEmpty)
                _deviceLink.SendEmptySignal(uid, comp.SignalOutputB, source);
            else
                _deviceLink.SendSignal(uid, comp.SignalOutputB, signalOutputB, source);
        }

        // Update power-related outputs
        UpdatePowerOutputs(uid, comp, source);
    }

    private void UpdatePowerOutputs(EntityUid uid, PowerRelayComponent comp, DeviceLinkSourceComponent source)
    {
        // Get power information if available
        float loadDemand = 0;
        float actualPower = 0;

        if (TryComp<PowerConsumerComponent>(uid, out var consumer))
        {
            loadDemand = consumer.DrawRate;
            actualPower = Math.Min(loadDemand, comp.MaxPowerFlow);
            
            // If relay is inactive, no power flows
            if (!comp.IsActive)
                actualPower = 0;
        }

        // Send load output
        var loadOutput = LogicSignalData.Numeric(loadDemand);
        if (comp.LastLoadOutput == null || !comp.LastLoadOutput.Equals(loadOutput))
        {
            comp.LastLoadOutput = loadOutput;
            _deviceLink.SendSignal(uid, comp.LoadOutputPort, loadOutput, source);
        }

        // Send power output
        var powerOutput = LogicSignalData.Numeric(actualPower);
        if (comp.LastPowerOutput == null || !comp.LastPowerOutput.Equals(powerOutput))
        {
            comp.LastPowerOutput = powerOutput;
            _deviceLink.SendSignal(uid, comp.PowerOutputPort, powerOutput, source);
        }
    }
}