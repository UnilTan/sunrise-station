using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared.CodeLock;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.CodeLock;

/// <summary>
/// System responsible for assigning department codes to the appropriate department heads.
/// </summary>
public sealed class DepartmentCodeAssignmentSystem : EntitySystem
{
    [Dependency] private readonly CodeLockSystem _codelock = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    /// <summary>
    /// Tracks department codes that have been assigned this round.
    /// Key: Job prototype ID, Value: (Code, Locker Entity)
    /// </summary>
    private readonly Dictionary<ProtoId<JobPrototype>, (string Code, EntityUid Locker)> _assignedCodes = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<DepartmentCodeLockComponent, MapInitEvent>(OnDepartmentCodeLockMapInit);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        // Clear any previous round data
        _assignedCodes.Clear();
        
        // Initialize all department code locks
        var query = EntityQueryEnumerator<DepartmentCodeLockComponent, CodeLockComponent>();
        while (query.MoveNext(out var uid, out var deptLock, out var codeLock))
        {
            // Generate a unique code for this locker
            var code = _codelock.GenerateRandomCode(codeLock.CodeLength);
            
            // Set the code on the lock
            _codelock.SetCode(uid, code, codeLock);
            
            // Track the assignment
            deptLock.AssignedCode = code;
            deptLock.CodeAssigned = false;
            _assignedCodes[deptLock.TargetJob] = (code, uid);
            
            Dirty(uid, deptLock);
        }
    }

    private void OnDepartmentCodeLockMapInit(EntityUid uid, DepartmentCodeLockComponent component, MapInitEvent args)
    {
        // Ensure this entity also has a CodeLock component
        if (!HasComp<CodeLockComponent>(uid))
        {
            var codeLock = AddComp<CodeLockComponent>(uid);
            
            // Configure some reasonable defaults for department lockers
            codeLock.CodeLength = 4;
            codeLock.UnlockDuration = 30f; // Auto-relock after 30 seconds
            codeLock.MaxFailedAttempts = 3;
            codeLock.LockoutDuration = 60f; // 1 minute lockout after too many failed attempts
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mindId, out var mind))
            return;

        // Check if this player has a job that should receive a department code
        if (!_jobs.MindTryGetJob(mindId, out var jobProto))
            return;

        var jobId = jobProto.ID;
        if (_assignedCodes.TryGetValue(jobId, out var codeData) && 
            TryComp<DepartmentCodeLockComponent>(codeData.Locker, out var deptLock) &&
            !deptLock.CodeAssigned)
        {
            // Assign the code to this player
            ShowCodeToPlayer(ev.Player, jobId, codeData.Code, codeData.Locker);
            deptLock.CodeAssigned = true;
            Dirty(codeData.Locker, deptLock);
        }
    }

    /// <summary>
    /// Shows the department locker code to a player.
    /// </summary>
    private void ShowCodeToPlayer(ICommonSession player, ProtoId<JobPrototype> jobId, string code, EntityUid locker)
    {
        if (!_prototypeManager.TryIndex(jobId, out var jobProto))
            return;

        var jobName = Loc.GetString(jobProto.Name);
        var lockerName = MetaData(locker).EntityName;
        
        var message = Loc.GetString("department-code-assignment", 
            ("job", jobName), 
            ("locker", lockerName), 
            ("code", code));

        _chatSystem.DispatchGlobalAnnouncement(message, colorOverride: Color.Green);
        
        // TODO: Instead of announcing to all, we should send this as a private message
        // or add it to the character's notes/PDA. For now, this allows testing.
    }

    /// <summary>
    /// Gets the assigned code for a specific job, if any.
    /// </summary>
    public string? GetAssignedCode(ProtoId<JobPrototype> jobId)
    {
        return _assignedCodes.TryGetValue(jobId, out var codeData) ? codeData.Code : null;
    }

    /// <summary>
    /// Gets all assigned department codes (for admin/debug purposes).
    /// </summary>
    public IReadOnlyDictionary<ProtoId<JobPrototype>, (string Code, EntityUid Locker)> GetAllAssignedCodes()
    {
        return _assignedCodes;
    }
}