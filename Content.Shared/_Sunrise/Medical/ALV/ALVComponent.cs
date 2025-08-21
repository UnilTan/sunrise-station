using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Medical.ALV;

/// <summary>
/// Component for tracking Artificial Lung Ventilation (ALV) state
/// Система искусственной вентиляции легких (ИВЛ)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ALVComponent : Component
{
    /// <summary>
    /// The entity performing the ALV procedure
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Performer;

    /// <summary>
    /// The entity receiving the ALV procedure
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Target;

    /// <summary>
    /// How much asphyxiation damage to heal per second (1.2-2.0 range)
    /// </summary>
    [DataField("healPerSecond")]
    public float HealPerSecond = 1.6f;

    /// <summary>
    /// How much mechanical damage to deal per second (0.2-0.5 range)
    /// </summary>
    [DataField("damagePerSecond")]
    public float DamagePerSecond = 0.35f;

    /// <summary>
    /// Sound played during ALV procedure
    /// </summary>
    [DataField("alvSound")]
    public SoundSpecifier? ALVSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// Tracks when we last applied ALV effects
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastEffectTime;

    /// <summary>
    /// How often to apply effects (in seconds)
    /// </summary>
    [DataField("effectInterval")]
    public float EffectInterval = 1.0f;
}