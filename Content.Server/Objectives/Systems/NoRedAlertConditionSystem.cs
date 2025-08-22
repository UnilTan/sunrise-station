using Content.Server.AlertLevel;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Station.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Timing;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the no red alert condition logic.
/// </summary>
public sealed class NoRedAlertConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, float> _redAlertStartTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoRedAlertConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        SubscribeLocalEvent<NoRedAlertConditionComponent, ObjectiveAfterAssignEvent>(OnObjectiveAssigned);
    }

    private void OnObjectiveAssigned(EntityUid uid, NoRedAlertConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        // Find the station this objective belongs to
        if (args.Mind.OwnedEntity != null)
        {
            comp.TrackedStation = _station.GetOwningStation(args.Mind.OwnedEntity.Value);
        }
    }

    private void OnGetProgress(EntityUid uid, NoRedAlertConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        // If we've exceeded the max red alert time, objective is failed
        args.Progress = comp.TotalRedAlertTime > comp.MaxRedAlertTime ? 0f : 1f;
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        var stationUid = ev.Station;
        
        // Check if any red alert time objectives are tracking this station
        var query = EntityQueryEnumerator<NoRedAlertConditionComponent>();
        while (query.MoveNext(out var objectiveUid, out var comp))
        {
            if (comp.TrackedStation != stationUid)
                continue;

            // If switching TO red alert, start tracking time
            if (ev.AlertLevel == "red")
            {
                _redAlertStartTimes[stationUid] = (float)_timing.CurTime.TotalSeconds;
            }
            // If switching FROM red alert, add the elapsed time
            else if (_redAlertStartTimes.TryGetValue(stationUid, out var startTime))
            {
                var elapsedTime = (float)_timing.CurTime.TotalSeconds - startTime;
                comp.TotalRedAlertTime += elapsedTime;
                _redAlertStartTimes.Remove(stationUid);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Update ongoing red alert times
        var currentTime = (float)_timing.CurTime.TotalSeconds;
        var query = EntityQueryEnumerator<NoRedAlertConditionComponent>();
        while (query.MoveNext(out var objectiveUid, out var comp))
        {
            if (comp.TrackedStation == null || !_redAlertStartTimes.TryGetValue(comp.TrackedStation.Value, out var startTime))
                continue;

            // Check if we're still on red alert
            if (TryComp<AlertLevelComponent>(comp.TrackedStation.Value, out var alertLevel) && 
                alertLevel.CurrentLevel == "red")
            {
                var elapsedTime = currentTime - startTime;
                // Update start time to current to avoid double counting
                _redAlertStartTimes[comp.TrackedStation.Value] = currentTime;
                comp.TotalRedAlertTime += frameTime;
            }
        }
    }
}