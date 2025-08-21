using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// A telepad that can receive teleported entities from linked teleporters.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResearchTelepadComponent : Component
{
    /// <summary>
    /// Whether this telepad is currently active and can receive teleports.
    /// </summary>
    [DataField]
    public bool Active = true;
}

/// <summary>
/// A teleporter that can send entities to linked telepads.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResearchTeleporterComponent : Component
{
    /// <summary>
    /// Maximum range for teleportation in tiles.
    /// </summary>
    [DataField]
    public float TeleportRange = 50f;

    /// <summary>
    /// Whether this teleporter is currently active.
    /// </summary>
    [DataField]
    public bool Active = true;

    /// <summary>
    /// Power required for each teleportation.
    /// </summary>
    [DataField]
    public float TeleportPowerCost = 10000f;
}