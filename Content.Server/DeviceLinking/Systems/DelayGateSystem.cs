using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling delay logic gates.
/// Implements the "Задержка" component behavior from the issue description.
/// </summary>
public sealed class DelayGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DelayGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<DelayGateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DelayGateComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DelayGateComponent, DeviceLinkSourceComponent>();
        
        while (query.MoveNext(out var uid, out var comp, out var source))
        {
            ProcessDelayedSignals(uid, comp, source, currentTime);
        }
    }

    private void OnInit(EntityUid uid, DelayGateComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);

        comp.CurrentOutput = LogicSignalData.Empty();
    }

    private void OnExamined(EntityUid uid, DelayGateComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("delay-gate-examine", 
            ("delay", comp.DelayTime),
            ("reset-signal", comp.ResetOnSignal ? "enabled" : "disabled"),
            ("reset-change", comp.ResetOnChange ? "enabled" : "disabled")));
    }

    private void OnInteractUsing(EntityUid uid, DelayGateComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, "Screwing"))
            return;

        // Cycle through delay modes
        if (comp.ResetOnSignal && comp.ResetOnChange)
        {
            comp.ResetOnSignal = false;
            comp.ResetOnChange = false;
        }
        else if (comp.ResetOnSignal)
        {
            comp.ResetOnChange = true;
        }
        else if (comp.ResetOnChange)
        {
            comp.ResetOnSignal = false;
            comp.ResetOnChange = false;
        }
        else
        {
            comp.ResetOnSignal = true;
        }

        var mode = GetModeDescription(comp);
        _popup.PopupEntity(Loc.GetString("delay-gate-mode-changed", ("mode", mode)), uid, args.User);
        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, DelayGateComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.InputPort)
            return;

        var signal = LogicSignalData.FromNetworkPayload(args.Data);
        var currentTime = _timing.CurTime;
        var outputTime = currentTime + TimeSpan.FromSeconds(comp.DelayTime);

        // Handle different delay modes
        if (comp.ResetOnSignal || comp.ResetOnChange)
        {
            var isSignalChange = comp.LastInput == null || !comp.LastInput.Equals(signal);

            if ((comp.ResetOnSignal && !signal.IsEmpty) || (comp.ResetOnChange && isSignalChange))
            {
                // Clear queue and reset
                comp.SignalQueue.Clear();
                comp.DelayStartTime = currentTime;
                
                if (comp.ResetOnSignal)
                {
                    // Output empty signal immediately, then queue the actual signal
                    _deviceLink.SendEmptySignal(uid);
                    comp.CurrentOutput = LogicSignalData.Empty();

                    // Only queue the signal if it's not empty
                    if (!signal.IsEmpty)
                    {
                        var delayedSignal = new DelayedSignal(signal, outputTime, TimeSpan.FromTicks(1));
                        comp.SignalQueue.Enqueue(delayedSignal);
                    }
                }
                else if (comp.ResetOnChange)
                {
                    // Output empty during delay, then output the final signal
                    _deviceLink.SendEmptySignal(uid);
                    comp.CurrentOutput = LogicSignalData.Empty();
                    
                    var delayedSignal = new DelayedSignal(signal, outputTime, TimeSpan.FromTicks(1));
                    comp.SignalQueue.Enqueue(delayedSignal);
                }
            }
        }
        else
        {
            // Normal delay mode - queue the signal with its duration
            var duration = TimeSpan.FromSeconds(0.017); // Default tick duration
            var delayedSignal = new DelayedSignal(signal, outputTime, duration);
            comp.SignalQueue.Enqueue(delayedSignal);
        }

        comp.LastInput = signal;
    }

    private void ProcessDelayedSignals(EntityUid uid, DelayGateComponent comp, DeviceLinkSourceComponent source, TimeSpan currentTime)
    {
        // Process signals that are ready to output
        while (comp.SignalQueue.Count > 0)
        {
            var delayedSignal = comp.SignalQueue.Peek();
            
            if (currentTime >= delayedSignal.OutputTime)
            {
                comp.SignalQueue.Dequeue();
                
                // Output the signal
                if (delayedSignal.Signal.IsEmpty)
                    _deviceLink.SendEmptySignal(uid, comp.OutputPort, source);
                else
                    _deviceLink.SendSignal(uid, comp.OutputPort, delayedSignal.Signal, source);
                
                comp.CurrentOutput = delayedSignal.Signal;
                
                // For impulse mode, schedule the signal to turn off after its duration
                if (comp.ResetOnSignal && !delayedSignal.Signal.IsEmpty)
                {
                    var endTime = delayedSignal.OutputTime + delayedSignal.Duration;
                    var endSignal = new DelayedSignal(LogicSignalData.Empty(), endTime, TimeSpan.Zero);
                    comp.SignalQueue.Enqueue(endSignal);
                }
            }
            else
            {
                break; // No more signals ready to output
            }
        }
    }

    private string GetModeDescription(DelayGateComponent comp)
    {
        if (comp.ResetOnSignal && comp.ResetOnChange)
            return Loc.GetString("delay-gate-mode-both");
        else if (comp.ResetOnSignal)
            return Loc.GetString("delay-gate-mode-reset-signal");
        else if (comp.ResetOnChange)
            return Loc.GetString("delay-gate-mode-reset-change");
        else
            return Loc.GetString("delay-gate-mode-normal");
    }
}