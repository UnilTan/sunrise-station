using Content.Shared.Physics;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Handles special recoil mechanics for entities with WeakEntityRecoilComponent.
/// </summary>
public sealed class WeakEntityRecoilSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeakEntityRecoilComponent, WeakEntityRecoilEvent>(OnWeakEntityRecoil);
    }

    private void OnWeakEntityRecoil(EntityUid uid, WeakEntityRecoilComponent component, WeakEntityRecoilEvent args)
    {
        // Check if weapon recoil exceeds threshold
        if (args.WeaponRecoil <= component.RecoilThreshold)
            return;

        // Apply stun effect
        _statusEffects.TryAddStatusEffect(uid, "Stun", TimeSpan.FromSeconds(component.StunDuration), true);

        // Apply knockback force
        var knockbackForce = component.KnockbackMultiplier * args.WeaponRecoil;
        var direction = (args.FromCoordinates.Position - args.ToCoordinates.Position).Normalized();

        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            var impulse = direction * knockbackForce;
            _physics.ApplyLinearImpulse(uid, impulse, body: physics);
        }
    }

    /// <summary>
    /// Processes weak entity recoil effects for the given user and weapon.
    /// </summary>
    public void ProcessWeakEntityRecoil(EntityUid user, float weaponRecoil, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates)
    {
        if (!HasComp<WeakEntityRecoilComponent>(user))
            return;

        var recoilEvent = new WeakEntityRecoilEvent(weaponRecoil, fromCoordinates, toCoordinates);
        RaiseLocalEvent(user, ref recoilEvent);
    }
}

/// <summary>
/// Event raised when a weak entity fires a weapon with recoil.
/// </summary>
[ByRefEvent]
public readonly record struct WeakEntityRecoilEvent(float WeaponRecoil, EntityCoordinates FromCoordinates, EntityCoordinates ToCoordinates);