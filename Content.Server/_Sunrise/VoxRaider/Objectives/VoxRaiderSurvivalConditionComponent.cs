namespace Content.Server._Sunrise.VoxRaider.Objectives;

/// <summary>
/// Requires that all Vox Raiders survive and escape to their shuttle/outpost.
/// </summary>
[RegisterComponent, Access(typeof(VoxRaiderSurvivalConditionSystem))]
public sealed partial class VoxRaiderSurvivalConditionComponent : Component
{
}