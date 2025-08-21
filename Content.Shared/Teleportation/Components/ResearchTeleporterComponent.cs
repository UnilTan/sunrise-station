using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

public enum TeleporterMode
{
    Transmit,
    Receive,
    Bidirectional
}

/// <summary>
/// A teleporter that can send and/or receive entities via device linking.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResearchTeleporterComponent : Component
{
    /// <summary>
    /// The mode of this teleporter (Transmit, Receive, or Bidirectional).
    /// </summary>
    [DataField]
    public TeleporterMode Mode = TeleporterMode.Transmit;

    /// <summary>
    /// Whether this teleporter is currently active.
    /// </summary>
    [DataField]
    public bool Active = true;

    /// <summary>
    /// Power required for each teleportation.
    /// </summary>
    [DataField]
    public float TeleportPowerCost = 10000f;

    /// <summary>
    /// Cooldown time between teleportations in seconds.
    /// </summary>
    [DataField]
    public float CooldownTime = 3f;

    /// <summary>
    /// Time when the teleporter will be ready for next use.
    /// </summary>
    [DataField]
    public TimeSpan NextUseTime;
}