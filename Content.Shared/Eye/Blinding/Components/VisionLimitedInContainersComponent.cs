using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// Component that marks entities that should have limited vision when inside containers or disposal tubes.
/// When the entity is inside a container or disposal, they will have blurred vision similar to welding without protection.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VisionLimitedInContainersComponent : Component
{
    /// <summary>
    /// The magnitude of blur applied when in containers
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blurMagnitude"), AutoNetworkedField]
    public float BlurMagnitude = 3.0f;

    /// <summary>
    /// Whether the entity is currently experiencing limited vision due to being in a container
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool IsLimitedVision = false;
}