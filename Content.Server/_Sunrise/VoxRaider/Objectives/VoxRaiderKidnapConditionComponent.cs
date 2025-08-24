namespace Content.Server._Sunrise.VoxRaider.Objectives;

/// <summary>
/// Requires that Vox Raiders kidnap crew members and bring them to their outpost.
/// </summary>
[RegisterComponent, Access(typeof(VoxRaiderKidnapConditionSystem))]
public sealed partial class VoxRaiderKidnapConditionComponent : Component
{
    /// <summary>
    /// Number of crew members that need to be kidnapped
    /// </summary>
    [DataField]
    public int RequiredKidnaps = 1;
}