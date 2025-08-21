using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling enhanced memory with locking capability.
/// Implements the "Память" component behavior from the issue description.
/// </summary>
public sealed class EnhancedMemorySystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnhancedMemoryComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EnhancedMemoryComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<EnhancedMemoryComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EnhancedMemoryComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Always output the stored value
        var query = EntityQueryEnumerator<EnhancedMemoryComponent, DeviceLinkSourceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var source))
        {
            UpdateOutput((uid, comp, source));
        }
    }

    private void OnInit(EntityUid uid, EnhancedMemoryComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort, comp.LockStatePort);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);

        comp.LastOutput = LogicSignalData.String(comp.StoredValue);
    }

    private void OnExamined(EntityUid uid, EnhancedMemoryComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var status = comp.AcceptingInput ? "accepting" : "locked";
        args.PushMarkup(Loc.GetString("enhanced-memory-examine", 
            ("value", comp.StoredValue), 
            ("status", Loc.GetString($"enhanced-memory-{status}"))));
    }

    private void OnInteractUsing(EntityUid uid, EnhancedMemoryComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, "Screwing"))
            return;

        // Allow manual editing of the stored value
        // In a real implementation, this would open a UI
        _popup.PopupEntity(Loc.GetString("enhanced-memory-manual-edit"), uid, args.User);
        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, EnhancedMemoryComponent comp, ref SignalReceivedEvent args)
    {
        var signal = LogicSignalData.FromNetworkPayload(args.Data);

        if (args.Port == comp.LockStatePort)
        {
            // Handle lock state control
            if (!signal.IsEmpty)
            {
                var value = signal.GetStringValue();
                comp.AcceptingInput = value == "1";
            }
            // Empty signal doesn't change the lock state
        }
        else if (args.Port == comp.InputPort)
        {
            // Handle input data
            if (comp.AcceptingInput && !signal.IsEmpty)
            {
                comp.StoredValue = signal.GetStringValue();
            }
        }
    }

    private void UpdateOutput(Entity<EnhancedMemoryComponent, DeviceLinkSourceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        // Always output the stored value (never empty as per the issue description)
        var output = LogicSignalData.String(ent.Comp1.StoredValue);

        // Only send if output actually changed
        if (ent.Comp1.LastOutput == null || !ent.Comp1.LastOutput.Equals(output))
        {
            ent.Comp1.LastOutput = output;
            _deviceLink.SendSignal(ent, ent.Comp1.OutputPort, output, ent.Comp2);
        }
    }
}