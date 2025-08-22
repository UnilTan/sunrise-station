using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the station not be on red alert for more than the specified time limit.
/// </summary>
[RegisterComponent, Access(typeof(NoRedAlertConditionSystem))]
public sealed partial class NoRedAlertConditionComponent : Component
{
    /// <summary>
    /// Maximum allowed time in seconds for red alert. Default 10 minutes (600 seconds).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxRedAlertTime = 600f;

    /// <summary>
    /// Total accumulated time the station has been on red alert.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TotalRedAlertTime = 0f;

    /// <summary>
    /// The station entity this objective is tracking.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? TrackedStation = null;
}