using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// A teleporter that has limited charges and teleports the user to a random location within a specified range.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChargedTeleporterComponent : Component
{
    /// <summary>
    /// Current number of charges available.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Charges = 3;

    /// <summary>
    /// Maximum number of charges.
    /// </summary>
    [DataField]
    public int MaxCharges = 3;

    /// <summary>
    /// Time in seconds to recharge one charge.
    /// </summary>
    [DataField]
    public float RechargeTime = 120f; // 2 minutes per charge

    /// <summary>
    /// Maximum range for random teleportation in tiles.
    /// </summary>
    [DataField]
    public float TeleportRange = 30f;

    /// <summary>
    /// Sound played when teleporting.
    /// </summary>
    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");

    /// <summary>
    /// Sound played when attempting to use with no charges.
    /// </summary>
    [DataField]
    public SoundSpecifier NoChargeSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>
    /// Time until next charge is restored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextChargeTime;
}

[Serializable, NetSerializable]
public sealed partial class ChargedTeleporterDoAfterEvent : SimpleDoAfterEvent
{
}