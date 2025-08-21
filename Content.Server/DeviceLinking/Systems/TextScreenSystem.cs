using Content.Server.DeviceLinking.Components;
using Content.Server.PDA.Ringer;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.PDA;
using Robust.Shared.Audio.Systems;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// System for handling text screen display logic.
/// Implements the "Текстовый экран" component behavior from the issue description.
/// </summary>
public sealed class TextScreenSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TextScreenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TextScreenComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, TextScreenComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);
        
        // Update appearance with initial text
        UpdateAppearance(uid, comp);
    }

    private void OnSignalReceived(EntityUid uid, TextScreenComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.InputPort)
            return;

        var signal = LogicSignalData.FromNetworkPayload(args.Data);

        // Don't update display for empty signals, as described in the issue
        if (signal.IsEmpty)
            return;

        // Update the displayed text
        var newText = signal.GetStringValue();
        
        // Truncate if too long
        if (newText.Length > comp.MaxDisplayLength)
            newText = newText[..comp.MaxDisplayLength];

        comp.DisplayText = newText;
        UpdateAppearance(uid, comp);
    }

    private void UpdateAppearance(EntityUid uid, TextScreenComponent comp)
    {
        // Update appearance to show the text on the screen
        // This would typically update a visual component or send data to clients
        _appearance.SetData(uid, PdaVisuals.ScreenText, comp.DisplayText);
    }
}