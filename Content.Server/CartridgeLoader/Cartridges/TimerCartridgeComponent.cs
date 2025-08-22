using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class TimerCartridgeComponent : Component
{
    /// <summary>
    /// The list of timers that have been created
    /// </summary>
    [DataField("timers")]
    public List<TimerEntry> Timers = new();
}