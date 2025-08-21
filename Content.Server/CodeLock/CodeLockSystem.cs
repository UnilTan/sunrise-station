using Content.Server.Storage.EntitySystems;
using Content.Shared.CodeLock;
using Content.Shared.Storage.Components;

namespace Content.Server.CodeLock;

/// <summary>
/// Server-side system for code lock functionality.
/// </summary>
public sealed class CodeLockSystem : SharedCodeLockSystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CodeLockComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CodeLockComponent, CodeLockUnlockedEvent>(OnCodeLockUnlocked);
        SubscribeLocalEvent<CodeLockComponent, CodeLockLockedEvent>(OnCodeLockLocked);
    }

    private void OnMapInit(EntityUid uid, CodeLockComponent component, MapInitEvent args)
    {
        // Generate a random code if none is set
        if (string.IsNullOrEmpty(component.Code))
        {
            component.Code = GenerateRandomCode(component.CodeLength);
        }

        // Start locked by default
        SetLocked(uid, true, component);
    }

    private void OnCodeLockUnlocked(EntityUid uid, CodeLockComponent component, ref CodeLockUnlockedEvent args)
    {
        // If this is attached to an entity storage, allow it to be opened
        if (TryComp<SharedEntityStorageComponent>(uid, out var storage) && !storage.Open)
        {
            _entityStorage.OpenStorage(uid, storage);
        }
    }

    private void OnCodeLockLocked(EntityUid uid, CodeLockComponent component, ref CodeLockLockedEvent args)
    {
        // If this is attached to an entity storage, force it to close
        if (TryComp<SharedEntityStorageComponent>(uid, out var storage) && storage.Open)
        {
            _entityStorage.CloseStorage(uid, storage);
        }
    }

    /// <summary>
    /// Server-side method to get the code (only available on server).
    /// </summary>
    public override string? GetCode(EntityUid uid, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.Code;
    }

    /// <summary>
    /// Forces the lock to unlock (admin/debug use).
    /// </summary>
    public void ForceUnlock(EntityUid uid, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetLocked(uid, false, component);
        component.FailedAttempts = 0;
        component.LockoutEndTime = null;
        component.EnteredCode = "";
        Dirty(uid, component);
        UpdateUserInterface(uid, component);

        var unlockEvent = new CodeLockUnlockedEvent(EntityUid.Invalid);
        RaiseLocalEvent(uid, ref unlockEvent);
    }

    /// <summary>
    /// Forces the lock to lock (admin/debug use).
    /// </summary>
    public void ForceLock(EntityUid uid, CodeLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetLocked(uid, true, component);
        component.EnteredCode = "";
        component.RelockTime = null;
        Dirty(uid, component);
        UpdateUserInterface(uid, component);

        var lockEvent = new CodeLockLockedEvent(EntityUid.Invalid);
        RaiseLocalEvent(uid, ref lockEvent);
    }
}