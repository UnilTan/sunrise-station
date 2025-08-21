using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.CodeLock;

/// <summary>
/// Shared system for code lock functionality.
/// </summary>
public abstract class SharedCodeLockSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterface = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CodeLockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CodeLockComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<CodeLockComponent, CodeLockKeypadPressedMessage>(OnKeypadPressed);
        SubscribeLocalEvent<CodeLockComponent, CodeLockKeypadClearMessage>(OnKeypadClear);
        SubscribeLocalEvent<CodeLockComponent, CodeLockKeypadEnterMessage>(OnKeypadEnter);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CodeLockComponent>();
        var currentTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var codeLock))
        {
            // Check if lock should auto-relock
            if (codeLock.RelockTime.HasValue && currentTime >= codeLock.RelockTime.Value)
            {
                SetLocked(uid, true, codeLock);
                codeLock.RelockTime = null;
                Dirty(uid, codeLock);
            }

            // Check if lockout should end
            if (codeLock.LockoutEndTime.HasValue && currentTime >= codeLock.LockoutEndTime.Value)
            {
                codeLock.LockoutEndTime = null;
                codeLock.FailedAttempts = 0;
                Dirty(uid, codeLock);
                UpdateUserInterface(uid, codeLock);
            }
        }
    }

    private void OnExamined(EntityUid uid, CodeLockComponent component, ExaminedEvent args)
    {
        if (component.IsUnlocked)
        {
            args.PushMarkup(Loc.GetString("code-lock-examine-unlocked"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("code-lock-examine-locked"));
        }

        if (IsLockedOut(component))
        {
            var remainingTime = (int)(component.LockoutEndTime!.Value - Timing.CurTime).TotalSeconds;
            args.PushMarkup(Loc.GetString("code-lock-examine-locked-out", ("time", remainingTime)));
        }
    }

    private void OnActivateInWorld(EntityUid uid, CodeLockComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        OpenUserInterface(uid, args.User, component);
        args.Handled = true;
    }

    private void OnKeypadPressed(EntityUid uid, CodeLockComponent component, CodeLockKeypadPressedMessage args)
    {
        if (IsLockedOut(component))
            return;

        Audio.PlayPvs(component.KeypadPressSound, uid);

        if (component.EnteredCode.Length >= component.CodeLength)
            return;

        component.EnteredCode += args.Value.ToString();
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnKeypadClear(EntityUid uid, CodeLockComponent component, CodeLockKeypadClearMessage args)
    {
        if (IsLockedOut(component))
            return;

        Audio.PlayPvs(component.KeypadPressSound, uid);
        component.EnteredCode = "";
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnKeypadEnter(EntityUid uid, CodeLockComponent component, CodeLockKeypadEnterMessage args)
    {
        if (IsLockedOut(component))
            return;

        if (string.IsNullOrEmpty(component.EnteredCode))
            return;

        if (component.EnteredCode == component.Code)
        {
            // Correct code
            Audio.PlayPvs(component.AccessGrantedSound, uid);
            SetLocked(uid, false, component);
            component.FailedAttempts = 0;
            
            // Set auto-relock timer if configured
            if (component.UnlockDuration > 0)
            {
                component.RelockTime = Timing.CurTime + TimeSpan.FromSeconds(component.UnlockDuration);
            }

            var unlockEvent = new CodeLockUnlockedEvent(args.Actor);
            RaiseLocalEvent(uid, ref unlockEvent);
        }
        else
        {
            // Incorrect code
            Audio.PlayPvs(component.AccessDeniedSound, uid);
            component.FailedAttempts++;

            // Check if should trigger lockout
            if (component.FailedAttempts >= component.MaxFailedAttempts)
            {
                component.LockoutEndTime = Timing.CurTime + TimeSpan.FromSeconds(component.LockoutDuration);
            }

            var failedEvent = new CodeLockFailedAttemptEvent(args.Actor);
            RaiseLocalEvent(uid, ref failedEvent);
        }

        component.EnteredCode = "";
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }

    protected virtual void OpenUserInterface(EntityUid uid, EntityUid user, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        UserInterface.OpenUi(uid, CodeLockUiKey.Key, user);
        UpdateUserInterface(uid, component);
    }

    protected void UpdateUserInterface(EntityUid uid, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var isLockedOut = IsLockedOut(component);
        var remainingLockoutTime = 0;

        if (isLockedOut && component.LockoutEndTime.HasValue)
        {
            remainingLockoutTime = (int)(component.LockoutEndTime.Value - Timing.CurTime).TotalSeconds;
        }

        var state = new CodeLockUserInterfaceState(
            component.EnteredCode.Length,
            component.CodeLength,
            component.IsUnlocked,
            isLockedOut,
            remainingLockoutTime
        );

        UserInterface.SetUiState(uid, CodeLockUiKey.Key, state);
    }

    protected bool IsLockedOut(CodeLockComponent component)
    {
        return component.LockoutEndTime.HasValue && Timing.CurTime < component.LockoutEndTime.Value;
    }

    public virtual void SetLocked(EntityUid uid, bool locked, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsUnlocked == !locked)
            return;

        component.IsUnlocked = !locked;
        component.RelockTime = null;
        Dirty(uid, component);

        if (locked)
        {
            var lockEvent = new CodeLockLockedEvent(EntityUid.Invalid);
            RaiseLocalEvent(uid, ref lockEvent);
        }

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Generates a random numeric code of the specified length.
    /// </summary>
    public string GenerateRandomCode(int length)
    {
        var code = "";
        for (var i = 0; i < length; i++)
        {
            code += Random.Next(0, 10).ToString();
        }
        return code;
    }

    /// <summary>
    /// Sets the code for a code lock.
    /// </summary>
    public void SetCode(EntityUid uid, string code, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Code = code;
        component.CodeLength = code.Length;
        Dirty(uid, component);
    }

    /// <summary>
    /// Gets the code for a code lock (server-side only).
    /// </summary>
    public virtual string? GetCode(EntityUid uid, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.Code;
    }
}