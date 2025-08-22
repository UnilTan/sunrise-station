using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is never put on the wanted list during the round.
/// If the player is ever set to wanted status, this objective fails.
/// </summary>
[RegisterComponent, Access(typeof(NotWantedConditionSystem))]
public sealed partial class NotWantedConditionComponent : Component
{
    /// <summary>
    /// Whether the player has been wanted at any point during the round.
    /// Once set to true, this objective is permanently failed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool HasBeenWanted = false;
}