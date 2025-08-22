using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Species.Swine;

/// <summary>
/// System that handles the swine rage mechanics
/// </summary>
public sealed class SwineRageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to damage changes to check for rage activation
        SubscribeLocalEvent<SwineRageComponent, DamageChangedEvent>(OnDamageChanged);
        
        // Subscribe to melee attacks to build rage stacks
        SubscribeLocalEvent<SwineRageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SwineRageComponent>();
        
        while (query.MoveNext(out var uid, out var rage))
        {
            // Decay rage stacks over time
            DecayRageStacks(uid, rage, currentTime);
            
            // Check if rage should be deactivated
            if (rage.RageActive && rage.RageStacks == 0)
            {
                DeactivateRage(uid, rage);
            }
        }
    }

    private void OnDamageChanged(EntityUid uid, SwineRageComponent rage, DamageChangedEvent args)
    {
        // Simple health percentage calculation using total damage vs a fixed threshold
        var currentDamage = (float)args.Damageable.TotalDamage;
        var assumedMaxHealth = 100f; // Assume typical health threshold
        var healthPercentage = Math.Max(0, (assumedMaxHealth - currentDamage) / assumedMaxHealth * 100f);

        // Activate rage if below threshold and not already active
        if (healthPercentage <= rage.RageHealthThreshold && 
            !rage.RageActive && 
            rage.RageUsesThisRound < rage.MaxRageUsesPerRound)
        {
            ActivateRage(uid, rage);
        }
    }

    private void OnMeleeHit(EntityUid uid, SwineRageComponent rage, MeleeHitEvent args)
    {
        // Only build rage stacks if rage is active
        if (!rage.RageActive)
            return;

        // Add a rage stack
        AddRageStack(uid, rage);
    }

    private void ActivateRage(EntityUid uid, SwineRageComponent rage)
    {
        rage.RageActive = true;
        rage.RageUsesThisRound++;
        
        // Add initial rage stack
        AddRageStack(uid, rage);
        
        // Play rage activation scream
        _audio.PlayPredicted(new SoundCollectionSpecifier("SwineScreams"), uid, uid);
        
        // TODO: Add visual effects (red eyes, sprite shaking, screen filter)
        
        Dirty(uid, rage);
    }

    private void DeactivateRage(EntityUid uid, SwineRageComponent rage)
    {
        rage.RageActive = false;
        rage.RageStacks = 0;
        rage.RageStackTimes.Clear();
        
        // TODO: Remove visual effects
        
        Dirty(uid, rage);
    }

    private void AddRageStack(EntityUid uid, SwineRageComponent rage)
    {
        if (rage.RageStacks >= rage.MaxRageStacks)
            return;

        rage.RageStacks++;
        rage.RageStackTimes.Enqueue(_timing.CurTime);
        
        // TODO: Update visual effects based on rage level
        
        Dirty(uid, rage);
    }

    private void DecayRageStacks(EntityUid uid, SwineRageComponent rage, TimeSpan currentTime)
    {
        var decayThreshold = TimeSpan.FromSeconds(rage.RageDecayTime);
        
        while (rage.RageStackTimes.Count > 0)
        {
            var stackTime = rage.RageStackTimes.Peek();
            if (currentTime - stackTime < decayThreshold)
                break;
                
            rage.RageStackTimes.Dequeue();
            rage.RageStacks--;
            
            // Update visual effects
            Dirty(uid, rage);
        }
    }

    /// <summary>
    /// Gets the current damage multiplier based on rage stacks
    /// </summary>
    public float GetRageDamageMultiplier(EntityUid uid, SwineRageComponent? rage = null)
    {
        if (!Resolve(uid, ref rage) || !rage.RageActive)
            return 1.0f;

        return 1.0f + (rage.RageStacks * rage.DamageMultiplierPerStack);
    }

    /// <summary>
    /// Gets the current resistance multiplier based on rage stacks
    /// </summary>
    public float GetRageResistanceMultiplier(EntityUid uid, SwineRageComponent? rage = null)
    {
        if (!Resolve(uid, ref rage) || !rage.RageActive)
            return 1.0f;

        return 1.0f - (rage.RageStacks * rage.ResistanceMultiplierPerStack);
    }
}