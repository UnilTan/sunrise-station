using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared._Sunrise.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Salvage;

/// <summary>
/// System for managing enhanced salvage shuttle control
/// </summary>
public sealed class EnhancedSalvageShuttleControlSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<EnhancedSalvageShuttleControlComponent, ComponentInit>(OnComponentInit);
        
        Subs.BuiEvents<EnhancedSalvageShuttleControlComponent>(EnhancedSalvageShuttleControlUiKey.Key, subs =>
        {
            subs.Event<StartSalvageMissionMessage>(OnStartMission);
            subs.Event<EndSalvageMissionMessage>(OnEndMission);
            subs.Event<ActivateEmergencyModeMessage>(OnActivateEmergency);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EnhancedSalvageShuttleControlComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.MissionActive && component.MissionStartTime.HasValue)
            {
                var elapsed = _timing.CurTime - component.MissionStartTime.Value;
                var maxDuration = TimeSpan.FromMinutes(component.MaxMissionDurationMinutes);
                var warningTime = TimeSpan.FromMinutes(component.WarningTimeMinutes);

                if (elapsed >= maxDuration)
                {
                    // Auto return shuttle
                    EndMission(uid, component);
                }
                else if (elapsed >= maxDuration - warningTime)
                {
                    // Activate warning
                    if (component.WarningSound != null)
                    {
                        _audio.PlayPvs(component.WarningSound, uid);
                    }
                }

                UpdateUI(uid, component);
            }
        }
    }

    private void OnComponentInit(EntityUid uid, EnhancedSalvageShuttleControlComponent component, ComponentInit args)
    {
        UpdateUI(uid, component);
    }

    private void OnStartMission(EntityUid uid, EnhancedSalvageShuttleControlComponent component, StartSalvageMissionMessage args)
    {
        if (component.MissionActive)
            return;

        component.MissionActive = true;
        component.CurrentMissionType = args.MissionType;
        component.MissionStartTime = _timing.CurTime;
        component.EmergencyMode = false;

        UpdateUI(uid, component);
    }

    private void OnEndMission(EntityUid uid, EnhancedSalvageShuttleControlComponent component, EndSalvageMissionMessage args)
    {
        EndMission(uid, component);
    }

    private void OnActivateEmergency(EntityUid uid, EnhancedSalvageShuttleControlComponent component, ActivateEmergencyModeMessage args)
    {
        // TODO: Check for captain's card or access
        component.EmergencyMode = true;
        UpdateUI(uid, component);
    }

    private void EndMission(EntityUid uid, EnhancedSalvageShuttleControlComponent component)
    {
        component.MissionActive = false;
        component.MissionStartTime = null;
        component.EmergencyMode = false;

        UpdateUI(uid, component);
    }

    private void UpdateUI(EntityUid uid, EnhancedSalvageShuttleControlComponent component)
    {
        TimeSpan? timeRemaining = null;
        bool warningActive = false;

        if (component.MissionActive && component.MissionStartTime.HasValue)
        {
            var elapsed = _timing.CurTime - component.MissionStartTime.Value;
            var maxDuration = TimeSpan.FromMinutes(component.MaxMissionDurationMinutes);
            var warningTime = TimeSpan.FromMinutes(component.WarningTimeMinutes);

            timeRemaining = maxDuration - elapsed;
            warningActive = elapsed >= maxDuration - warningTime;
        }

        var state = new EnhancedSalvageShuttleControlState(
            component.MissionActive,
            component.CurrentMissionType,
            component.MissionStartTime,
            timeRemaining,
            warningActive,
            component.EmergencyMode,
            component.RequiresEmergencyCard
        );

        _ui.SetUiState(uid, EnhancedSalvageShuttleControlUiKey.Key, state);
    }
}