using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.CodeLock;

/// <summary>
/// Component that marks a locker as requiring a code that should be assigned to a specific job/department.
/// Used for department head lockers to generate and assign unique codes.
/// </summary>
[RegisterComponent]
public sealed partial class DepartmentCodeLockComponent : Component
{
    /// <summary>
    /// The job prototype ID that should receive the code for this locker.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> TargetJob = string.Empty;

    /// <summary>
    /// Whether the code has been assigned to a player this round.
    /// </summary>
    [DataField]
    public bool CodeAssigned = false;

    /// <summary>
    /// The assigned code for this round (server-side only).
    /// </summary>
    [DataField]
    public string AssignedCode = string.Empty;
}