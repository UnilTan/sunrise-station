using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the frame target condition logic.
/// </summary>
public sealed class FrameTargetConditionSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrameTargetConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, FrameTargetConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value);
    }

    private float GetProgress(EntityUid target)
    {
        // Check if target is dead or missing
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 0f;

        // Check if target is cuffed
        var isCuffed = TryComp<CuffableComponent>(mind.OwnedEntity.Value, out var cuffed) && cuffed.CuffedHandCount > 0;
        
        if (!isCuffed)
            return 0f;

        // If emergency shuttle hasn't arrived yet, give partial progress
        if (!_emergencyShuttle.EmergencyShuttleArrived)
            return 0.5f;

        // Full progress if they're still cuffed when shuttle is here/gone
        return 1f;
    }
}