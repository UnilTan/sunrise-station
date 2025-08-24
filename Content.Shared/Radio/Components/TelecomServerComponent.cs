namespace Content.Shared.Radio.Components;

/// <summary>
/// Entities with <see cref="TelecomServerComponent"/> are needed to transmit messages using headsets.
/// They also need to be powered by <see cref="ApcPowerReceiverComponent"/>
/// have <see cref="EncryptionKeyHolderComponent"/> and filled with encryption keys
/// of channels in order for them to work on the same map as server.
/// </summary>
[RegisterComponent]
public sealed partial class TelecomServerComponent : Component
{
    /// <summary>
    /// Current degradation level of the telecom server (0.0 = perfect, 1.0 = completely broken)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("degradation")]
    public float Degradation = 0.0f;

    /// <summary>
    /// Rate at which the telecom server degrades over time (per second)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("degradationRate")]
    public float DegradationRate = 0.0001f; // Very slow natural degradation

    /// <summary>
    /// Degradation added per radio message processed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("usageDegradation")]
    public float UsageDegradation = 0.005f;

    /// <summary>
    /// Maximum range at which this telecom server operates effectively
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxRange")]
    public float MaxRange = 50.0f;
}
