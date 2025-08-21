using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;

namespace Content.Server._Sunrise.NukeOps;

/// <summary>
/// Component for the nuclear operative communications jammer device
/// Blocks radio communications, fax transmissions, announcements, and ERT calls while powered
/// </summary>
[RegisterComponent]
public sealed partial class NukeOpsCommJammerComponent : Component
{
    /// <summary>
    /// Whether the jammer is currently active and jamming communications
    /// </summary>
    [DataField]
    public bool Active = false;

    /// <summary>
    /// Minimum power required for the jammer to function (in watts)
    /// </summary>
    [DataField]
    public float PowerRequirement = 5000f;

    /// <summary>
    /// Range in which the jammer blocks communications (in tiles)
    /// If 0, affects the entire station
    /// </summary>
    [DataField]
    public float Range = 0f;

    /// <summary>
    /// Sound played when the jammer is activated
    /// </summary>
    [DataField]
    public SoundSpecifier? ActivationSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    /// <summary>
    /// Sound played when the jammer is deactivated
    /// </summary>
    [DataField]
    public SoundSpecifier? DeactivationSound = new SoundPathSpecifier("/Audio/Machines/terminal_off.ogg");

    /// <summary>
    /// Whether the jammer only works when placed on a station
    /// </summary>
    [DataField]
    public bool RequiresStationPlacement = true;
}