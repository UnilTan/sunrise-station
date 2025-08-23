using Content.Client._Sunrise.PaintMixer.UI;
using Content.Shared._Sunrise.PaintMixer;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.PaintMixer;

/// <summary>
/// Bound user interface for the paint mixer machine
/// </summary>
public sealed class PaintMixerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaintMixerWindow? _window;

    public PaintMixerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaintMixerWindow>();
        _window.OnColorSelected += OnColorSelected;
        _window.OnMixRequested += OnMixRequested;

        // Request initial state
        SendMessage(new PaintMixerSyncRequestMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PaintMixerUpdateState updateState)
        {
            _window?.UpdateState(updateState);
        }
    }

    private void OnColorSelected(Color color)
    {
        SendMessage(new PaintMixerSetColorMessage(color));
    }

    private void OnMixRequested()
    {
        SendMessage(new PaintMixerStartMixingMessage());
    }
}