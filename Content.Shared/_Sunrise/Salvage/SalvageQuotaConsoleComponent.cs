using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Salvage;

/// <summary>
/// Консоль управления квотами утилизации
/// Manages salvage quotas and mission ordering
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SalvageQuotaConsoleComponent : Component
{
    /// <summary>
    /// Sound played when the console is used
    /// </summary>
    [DataField]
    public SoundSpecifier? ActivationSound = new SoundPathSpecifier("/Audio/Machines/terminal_select.ogg");
}

/// <summary>
/// Types of salvage missions available
/// </summary>
[Serializable, NetSerializable]
public enum SalvageMissionType : byte
{
    Money,      // Деньги
    Resources,  // Ресурсы
    Artifacts,  // Артефакты
    Mixed       // Смешанный
}

/// <summary>
/// State of the salvage quota console
/// </summary>
[Serializable, NetSerializable]
public sealed class SalvageQuotaConsoleState : BoundUserInterfaceState
{
    public SalvageMissionType SelectedMissionType;
    public bool MissionActive;
    public bool ShuttleAvailable;
    
    public SalvageQuotaConsoleState(SalvageMissionType selectedMissionType, bool missionActive, bool shuttleAvailable)
    {
        SelectedMissionType = selectedMissionType;
        MissionActive = missionActive;
        ShuttleAvailable = shuttleAvailable;
    }
}

/// <summary>
/// Message to select a mission type
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectSalvageMissionTypeMessage : BoundUserInterfaceMessage
{
    public SalvageMissionType MissionType;
    
    public SelectSalvageMissionTypeMessage(SalvageMissionType missionType)
    {
        MissionType = missionType;
    }
}

/// <summary>
/// Message to activate the salvage shuttle
/// </summary>
[Serializable, NetSerializable]
public sealed class ActivateSalvageShuttleMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// UI key for the salvage quota console
/// </summary>
[Serializable, NetSerializable]
public enum SalvageQuotaConsoleUiKey : byte
{
    Key
}