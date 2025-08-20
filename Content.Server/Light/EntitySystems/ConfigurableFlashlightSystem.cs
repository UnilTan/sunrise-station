using Content.Server.UserInterface;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems;

public sealed class ConfigurableFlashlightSystem : SharedConfigurableFlashlightSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConfigurableFlashlightComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ConfigurableFlashlightComponent, ConfigurableFlashlightSetColorMessage>(OnSetColor);
        SubscribeLocalEvent<ConfigurableFlashlightComponent, ConfigurableFlashlightResetMessage>(OnReset);
        SubscribeLocalEvent<ConfigurableFlashlightComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnGetVerbs(EntityUid uid, ConfigurableFlashlightComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var verb = new InteractionVerb()
        {
            Act = () => OpenUI(uid, args.User),
            Text = Loc.GetString("configurable-flashlight-verb-configure"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/color.svg.192dpi.png")),
            Priority = 10
        };

        args.Verbs.Add(verb);
    }

    private void OpenUI(EntityUid uid, EntityUid user)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _userInterface.OpenUi(uid, ConfigurableFlashlightUiKey.Key, actor.PlayerSession);
    }

    private void OnSetColor(EntityUid uid, ConfigurableFlashlightComponent component, ConfigurableFlashlightSetColorMessage args)
    {
        // Validate the color input
        if (!IsValidColor(args.Color))
        {
            return;
        }

        SetColor(uid, args.Color, component);
    }

    private void OnReset(EntityUid uid, ConfigurableFlashlightComponent component, ConfigurableFlashlightResetMessage args)
    {
        ResetToDefault(uid, component);
    }

    private void OnBoundUIOpened(EntityUid uid, ConfigurableFlashlightComponent component, BoundUIOpenedEvent args)
    {
        var state = new ConfigurableFlashlightBuiState(component.CustomColor, component.OriginalColor);
        _userInterface.SetUiState(uid, ConfigurableFlashlightUiKey.Key, state);
    }

    private bool IsValidColor(Color color)
    {
        // Basic validation - ensure color values are in valid ranges
        return color.R >= 0 && color.R <= 1 &&
               color.G >= 0 && color.G <= 1 &&
               color.B >= 0 && color.B <= 1 &&
               color.A >= 0 && color.A <= 1;
    }
}