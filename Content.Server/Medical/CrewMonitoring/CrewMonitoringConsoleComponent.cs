using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Audio;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    /// <summary>
    ///     Whether the console should beep when corpses with sensors are detected outside morgues.
    /// </summary>
    [DataField("doCorpseAlert"), ViewVariables(VVAccess.ReadWrite)]
    public bool DoCorpseAlert = true;

    /// <summary>
    ///     Accumulated frame time for corpse alert beeping.
    /// </summary>
    public float AccumulatedCorpseAlertTime = 0f;

    /// <summary>
    ///     The amount of time between each corpse alert beep.
    /// </summary>
    [DataField("corpseAlertTime"), ViewVariables(VVAccess.ReadWrite)]
    public float CorpseAlertTime = 15f;

    /// <summary>
    ///     Sound to play when corpses with sensors are detected outside morgues.
    /// </summary>
    [DataField("corpseAlertSound")]
    public SoundSpecifier CorpseAlertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
}
