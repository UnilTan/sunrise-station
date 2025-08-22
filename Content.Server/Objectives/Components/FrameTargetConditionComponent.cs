using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the target be arrested (in custody) by the end of the round.
/// Depends on TargetObjectiveComponent to function.
/// </summary>
[RegisterComponent, Access(typeof(FrameTargetConditionSystem))]
public sealed partial class FrameTargetConditionComponent : Component
{
}