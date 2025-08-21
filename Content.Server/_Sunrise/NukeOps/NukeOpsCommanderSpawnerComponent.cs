using Content.Server.GameTicking.Rules;
using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.NukeOps;

/// <summary>
/// Component to mark a spawner as being for the nuclear operative commander
/// </summary>
[RegisterComponent]
public sealed partial class NukeOpsCommanderSpawnerComponent : Component
{
    /// <summary>
    /// The associated nuclear operative rule
    /// </summary>
    [DataField]
    public EntityUid? AssociatedRule;
}