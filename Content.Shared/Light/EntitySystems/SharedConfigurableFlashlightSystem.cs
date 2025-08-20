using Content.Shared.Light.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedConfigurableFlashlightSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfigurableFlashlightComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ConfigurableFlashlightComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ConfigurableFlashlightComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ConfigurableFlashlightComponent component, ComponentStartup args)
    {
        // Store the original color when the component starts up
        if (_lights.TryGetLight(uid, out var light))
        {
            component.OriginalColor = light.Color;
        }
    }

    private void OnGetState(EntityUid uid, ConfigurableFlashlightComponent component, ref ComponentGetState args)
    {
        args.State = new ConfigurableFlashlightComponentState(component.CustomColor);
    }

    private void OnHandleState(EntityUid uid, ConfigurableFlashlightComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ConfigurableFlashlightComponentState state)
            return;

        SetColor(uid, state.CustomColor, component);
    }

    public virtual void SetColor(EntityUid uid, Color? color, ConfigurableFlashlightComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.CustomColor = color;

        if (_lights.TryGetLight(uid, out var light))
        {
            var targetColor = color ?? component.OriginalColor ?? Color.White;
            _lights.SetColor(uid, targetColor, light);
        }

        Dirty(uid, component);
    }

    public void ResetToDefault(EntityUid uid, ConfigurableFlashlightComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetColor(uid, null, component);
    }
}