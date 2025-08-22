using Content.Server.Objectives.Components;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the damage target condition logic.
/// </summary>
public sealed class DamageTargetConditionSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageTargetConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<DamageTargetConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(EntityUid uid, DamageTargetConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        // Store the initial damage when the objective is assigned
        if (!_target.GetTarget(uid, out var target))
            return;

        if (!TryComp<MindComponent>(target.Value, out var mind) || mind.OwnedEntity == null)
            return;

        if (TryComp<DamageableComponent>(mind.OwnedEntity.Value, out var damageable))
        {
            comp.InitialDamage = damageable.TotalDamage.Double();
        }
    }

    private void OnGetProgress(EntityUid uid, DamageTargetConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp);
    }

    private float GetProgress(EntityUid target, DamageTargetConditionComponent comp)
    {
        // Check if target is missing or dead (if they need to be alive)
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 0f;

        var targetEntity = mind.OwnedEntity.Value;
        
        // If target must be alive but is dead, objective failed
        if (comp.RequireAlive && _mobState.IsDead(targetEntity))
            return 0f;

        // Calculate damage dealt since objective started
        if (TryComp<DamageableComponent>(targetEntity, out var damageable))
        {
            var currentDamage = damageable.TotalDamage.Double();
            var damageDealt = currentDamage - comp.InitialDamage;
            
            // Return progress based on damage dealt vs required
            return Math.Min(1f, (float)(damageDealt / comp.RequiredDamage));
        }

        return 0f;
    }
}