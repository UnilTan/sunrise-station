using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.SlimeAppearance;

/// <summary>
/// Component that allows slime people to edit their appearance in-game.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeAppearanceComponent : Component
{
    /// <summary>
    /// Whether the slime appearance editor is currently enabled for this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}