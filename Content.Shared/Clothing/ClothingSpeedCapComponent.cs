using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing;

/// <summary>
/// Sets maximum speed limits that cannot be exceeded by any positive speed modifiers (e.g., stimulants).
/// This component defines absolute speed caps that are applied after all other modifiers are calculated.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ClothingSpeedCapSystem))]
public sealed partial class ClothingSpeedCapComponent : Component
{
    /// <summary>
    /// Maximum walking speed that cannot be exceeded.
    /// Set to null to disable walking speed cap.
    /// </summary>
    [DataField]
    public float? MaxWalkSpeed;

    /// <summary>
    /// Maximum sprinting speed that cannot be exceeded.
    /// Set to null to disable sprinting speed cap.
    /// </summary>
    [DataField]
    public float? MaxSprintSpeed;

    /// <summary>
    /// An optional required standing state.
    /// Set to true if caps only apply when standing, false if only when lying down, null for all states.
    /// </summary>
    [DataField]
    public bool? Standing;
}

[Serializable, NetSerializable]
public sealed class ClothingSpeedCapComponentState : ComponentState
{
    public float? MaxWalkSpeed;
    public float? MaxSprintSpeed;
    public bool? Standing;

    public ClothingSpeedCapComponentState(float? maxWalkSpeed, float? maxSprintSpeed, bool? standing)
    {
        MaxWalkSpeed = maxWalkSpeed;
        MaxSprintSpeed = maxSprintSpeed;
        Standing = standing;
    }
}