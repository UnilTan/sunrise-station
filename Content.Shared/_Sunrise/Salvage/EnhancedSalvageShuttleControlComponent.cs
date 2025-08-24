using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared._Sunrise.Salvage;

namespace Content.Shared._Sunrise.Salvage;

/// <summary>
/// Enhanced salvage shuttle control console with timer and progress tracking
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EnhancedSalvageShuttleControlComponent : Component
{
    /// <summary>
    /// Maximum mission duration in minutes
    /// </summary>
    [DataField]
    public float MaxMissionDurationMinutes = 30f;

    /// <summary>
    /// Warning time before auto return in minutes
    /// </summary>
    [DataField]
    public float WarningTimeMinutes = 2f;

    /// <summary>
    /// Emergency activation card requirement
    /// </summary>
    [DataField]
    public bool RequiresEmergencyCard = true;

    /// <summary>
    /// Is emergency mode active
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EmergencyMode = false;

    /// <summary>
    /// Current mission start time
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("missionStartTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? MissionStartTime;

    /// <summary>
    /// Is mission active
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool MissionActive = false;

    /// <summary>
    /// Current mission type
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public SalvageMissionType CurrentMissionType;

    /// <summary>
    /// Sound played when warning activates
    /// </summary>
    [DataField]
    public SoundSpecifier? WarningSound = new SoundPathSpecifier("/Audio/Ambience/Antag/carp_rift_emergency.ogg");

    /// <summary>
    /// Sound played during countdown
    /// </summary>
    [DataField]
    public SoundSpecifier? CountdownSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
}

/// <summary>
/// State for the enhanced salvage shuttle control console
/// </summary>
[Serializable, NetSerializable]
public sealed class EnhancedSalvageShuttleControlState : BoundUserInterfaceState
{
    public bool MissionActive;
    public SalvageMissionType CurrentMissionType;
    public TimeSpan? MissionStartTime;
    public TimeSpan? TimeRemaining;
    public bool WarningActive;
    public bool EmergencyMode;
    public bool CanActivateEmergency;
    
    public EnhancedSalvageShuttleControlState(
        bool missionActive,
        SalvageMissionType currentMissionType,
        TimeSpan? missionStartTime,
        TimeSpan? timeRemaining,
        bool warningActive,
        bool emergencyMode,
        bool canActivateEmergency)
    {
        MissionActive = missionActive;
        CurrentMissionType = currentMissionType;
        MissionStartTime = missionStartTime;
        TimeRemaining = timeRemaining;
        WarningActive = warningActive;
        EmergencyMode = emergencyMode;
        CanActivateEmergency = canActivateEmergency;
    }
}

/// <summary>
/// Message to start a mission
/// </summary>
[Serializable, NetSerializable]
public sealed class StartSalvageMissionMessage : BoundUserInterfaceMessage
{
    public SalvageMissionType MissionType;
    
    public StartSalvageMissionMessage(SalvageMissionType missionType)
    {
        MissionType = missionType;
    }
}

/// <summary>
/// Message to end a mission early
/// </summary>
[Serializable, NetSerializable]
public sealed class EndSalvageMissionMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Message to activate emergency mode
/// </summary>
[Serializable, NetSerializable]
public sealed class ActivateEmergencyModeMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// UI key for the enhanced salvage shuttle control console
/// </summary>
[Serializable, NetSerializable]
public enum EnhancedSalvageShuttleControlUiKey : byte
{
    Key
}