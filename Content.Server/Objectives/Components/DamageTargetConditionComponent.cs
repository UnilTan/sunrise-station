using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the target be dealt a specific amount of damage without killing them.
/// Depends on TargetObjectiveComponent to function.
/// </summary>
[RegisterComponent, Access(typeof(DamageTargetConditionSystem))]
public sealed partial class DamageTargetConditionComponent : Component
{
    /// <summary>
    /// Amount of damage that needs to be dealt to the target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RequiredDamage = 100f;

    /// <summary>
    /// Initial damage when the objective was assigned.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]  
    public double InitialDamage = 0.0;

    /// <summary>
    /// Whether the target must remain alive for the objective to succeed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireAlive = true;
}