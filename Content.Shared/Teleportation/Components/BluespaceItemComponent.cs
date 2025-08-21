using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Marks an item as being dangerous during bluespace teleportation.
/// Entities carrying bluespace items will die when teleported.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BluespaceItemComponent : Component
{
}