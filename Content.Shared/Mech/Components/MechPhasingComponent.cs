using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared.Mech.Components;

/// <summary>
/// A component for mechs that can phase through walls.
/// The phasing ability consumes energy while active.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechPhasingComponent : Component
{
    /// <summary>
    /// Whether the mech is currently phasing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Phasing = false;

    /// <summary>
    /// How much energy the phasing ability consumes per second.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 EnergyConsumption = 50;

    /// <summary>
    /// The alternative sprite state to use when phasing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PhaseState;

    /// <summary>
    /// Minimum energy required to activate phasing.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MinimumEnergy = 100;
}