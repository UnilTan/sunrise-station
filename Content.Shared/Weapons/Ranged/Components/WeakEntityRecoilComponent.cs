using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Marks an entity as weak to weapon recoil, causing stun and knockback effects
/// when firing weapons with high recoil values.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeakEntityRecoilComponent : Component
{
    /// <summary>
    /// Minimum weapon recoil threshold to trigger effects.
    /// </summary>
    [DataField]
    public float RecoilThreshold = 0.2f;

    /// <summary>
    /// Duration of stun effect in seconds.
    /// </summary>
    [DataField]
    public float StunDuration = 2.0f;

    /// <summary>
    /// Multiplier for knockback force calculation (force = multiplier * weapon recoil).
    /// </summary>
    [DataField]
    public float KnockbackMultiplier = 10.0f;
}