using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Security;
using Content.Shared.StationRecords;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the not wanted condition logic.
/// </summary>
public sealed class NotWantedConditionSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NotWantedConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, NotWantedConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        // Use the mind from the event instead of trying to get it from the objective
        var mind = args.Mind;
        var mindUid = args.MindId;

        if (mind.OwnedEntity == null)
        {
            args.Progress = 0f;
            return;
        }

        var playerEntity = mind.OwnedEntity.Value;
        var playerName = Identity.Name(playerEntity, EntityManager);
        var station = _station.GetOwningStation(playerEntity);

        if (station == null)
        {
            args.Progress = 1f; // If not on a station, can't be wanted
            return;
        }

        // Check if the player has a criminal record with wanted status
        foreach (var (key, record) in _records.GetRecordsOfType<CriminalRecord>(station.Value))
        {
            if (!_records.TryGetRecord<GeneralStationRecord>(new StationRecordKey(key, station.Value), out var generalRecord))
                continue;

            if (!string.Equals(generalRecord.Name?.Trim(), playerName?.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (record.Status == SecurityStatus.Wanted)
            {
                comp.HasBeenWanted = true;
                args.Progress = 0f;
                return;
            }
        }

        // If has been wanted before, still failed
        args.Progress = comp.HasBeenWanted ? 0f : 1f;
    }
}