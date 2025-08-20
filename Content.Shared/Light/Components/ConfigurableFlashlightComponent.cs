using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;

/// <summary>
/// Component that allows flashlights to have their light color configured by the user via HEX input.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConfigurableFlashlightComponent : Component
{
    /// <summary>
    /// The custom color set by the user. If null, uses the default light color.
    /// </summary>
    [DataField("customColor")]
    public Color? CustomColor { get; set; }

    /// <summary>
    /// The original color of the light before customization.
    /// Used to restore the default color when needed.
    /// </summary>
    public Color? OriginalColor { get; set; }
}

[Serializable, NetSerializable]
public sealed class ConfigurableFlashlightComponentState : ComponentState
{
    public Color? CustomColor { get; }

    public ConfigurableFlashlightComponentState(Color? customColor)
    {
        CustomColor = customColor;
    }
}