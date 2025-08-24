using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.VoxRaider;

/// <summary>
/// Added to mind role entities to tag that they are a vox raider.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoxRaiderRoleComponent : BaseMindRoleComponent
{
}