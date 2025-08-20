using Content.Client.Light.UI;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Client.UserInterface;

namespace Content.Client.Light.EntitySystems;

public sealed class ConfigurableFlashlightSystem : SharedConfigurableFlashlightSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfigurableFlashlightComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(EntityUid uid, ConfigurableFlashlightComponent component, ref AfterAutoHandleStateEvent args)
    {
        _userInterface.GetUIOrNull(uid, ConfigurableFlashlightUiKey.Key)?.Close();
    }

    public override void SetColor(EntityUid uid, Color? color, ConfigurableFlashlightComponent? component = null)
    {
        base.SetColor(uid, color, component);
        
        // Update the UI if it's open
        if (_userInterface.TryGetOpenUi(uid, ConfigurableFlashlightUiKey.Key, out var bui))
        {
            if (bui is ConfigurableFlashlightBoundUserInterface flashlightBui)
            {
                var state = new ConfigurableFlashlightBuiState(component?.CustomColor, component?.OriginalColor);
                flashlightBui.UpdateState(state);
            }
        }
    }
}