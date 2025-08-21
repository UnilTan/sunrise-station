using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CodeLock;

/// <summary>
/// A component that implements numeric code-based locking mechanism.
/// Used for department head lockers and other secure containers.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CodeLockComponent : Component
{
    /// <summary>
    /// The correct code needed to unlock this lock.
    /// </summary>
    [DataField]
    public string Code = string.Empty;

    /// <summary>
    /// The currently entered code by the user.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public string EnteredCode = string.Empty;

    /// <summary>
    /// The length of the code.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CodeLength = 4;

    /// <summary>
    /// Whether the lock is currently unlocked.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool IsUnlocked = false;

    /// <summary>
    /// Sound played when a button is pressed.
    /// </summary>
    [DataField]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    /// <summary>
    /// Sound played when access is granted (correct code entered).
    /// </summary>
    [DataField]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    /// <summary>
    /// Sound played when access is denied (incorrect code entered).
    /// </summary>
    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    /// <summary>
    /// How long the lock stays unlocked after correct code entry (in seconds).
    /// Set to 0 for permanent unlock until manually locked again.
    /// </summary>
    [DataField]
    public float UnlockDuration = 30f;

    /// <summary>
    /// Time when the lock will automatically relock (if UnlockDuration > 0).
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan? RelockTime;

    /// <summary>
    /// Maximum number of failed attempts before temporary lockout.
    /// </summary>
    [DataField]
    public int MaxFailedAttempts = 3;

    /// <summary>
    /// Current number of consecutive failed attempts.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int FailedAttempts = 0;

    /// <summary>
    /// How long to lock out after too many failed attempts (in seconds).
    /// </summary>
    [DataField]
    public float LockoutDuration = 60f;

    /// <summary>
    /// Time when the lockout ends.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan? LockoutEndTime;
}

/// <summary>
/// Network message for when a keypad button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class CodeLockKeypadPressedMessage : BoundUserInterfaceMessage
{
    public int Value;

    public CodeLockKeypadPressedMessage(int value)
    {
        Value = value;
    }
}

/// <summary>
/// Network message for when the clear button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class CodeLockKeypadClearMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Network message for when the enter button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class CodeLockKeypadEnterMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Network message for syncing the code lock state to the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class CodeLockUserInterfaceState : BoundUserInterfaceState
{
    public int EnteredCodeLength;
    public int MaxCodeLength;
    public bool IsUnlocked;
    public bool IsLockedOut;
    public int RemainingLockoutTime;

    public CodeLockUserInterfaceState(int enteredCodeLength, int maxCodeLength, bool isUnlocked, bool isLockedOut, int remainingLockoutTime)
    {
        EnteredCodeLength = enteredCodeLength;
        MaxCodeLength = maxCodeLength;
        IsUnlocked = isUnlocked;
        IsLockedOut = isLockedOut;
        RemainingLockoutTime = remainingLockoutTime;
    }
}

/// <summary>
/// UI key for the code lock interface.
/// </summary>
[Serializable, NetSerializable]
public enum CodeLockUiKey : byte
{
    Key,
}

/// <summary>
/// Events related to code lock functionality.
/// </summary>
[ByRefEvent]
public record struct CodeLockUnlockedEvent(EntityUid User);

[ByRefEvent]
public record struct CodeLockLockedEvent(EntityUid User);

[ByRefEvent]
public record struct CodeLockFailedAttemptEvent(EntityUid User);