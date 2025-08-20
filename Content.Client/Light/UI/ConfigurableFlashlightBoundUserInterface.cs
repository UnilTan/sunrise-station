using Content.Shared.Light.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Light.UI;

public sealed class ConfigurableFlashlightBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ConfigurableFlashlightWindow? _window;

    public ConfigurableFlashlightBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ConfigurableFlashlightWindow>();
        _window.Title = Loc.GetString("configurable-flashlight-window-title");

        _window.OnColorChanged += OnColorChanged;
        _window.OnResetPressed += OnResetPressed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ConfigurableFlashlightBuiState castState)
            return;

        _window?.UpdateState(castState);
    }

    private void OnColorChanged(Color color)
    {
        SendMessage(new ConfigurableFlashlightSetColorMessage(color));
    }

    private void OnResetPressed()
    {
        SendMessage(new ConfigurableFlashlightResetMessage());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (disposing)
        {
            _window?.Dispose();
        }
    }
}