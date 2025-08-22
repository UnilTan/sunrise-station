using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition;
using Content.Shared.Movement.Events;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.Species.Swine;

/// <summary>
/// System that handles swine species traits
/// </summary>
public sealed class SwineTraitsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to food ingestion events to provide healing
        SubscribeLocalEvent<SwineTraitsComponent, IngestedEvent>(OnFoodIngested);
        
        // Subscribe to digestibility checks to allow eating anything
        SubscribeLocalEvent<SwineTraitsComponent, IsDigestibleEvent>(OnDigestibilityCheck);
        
        // Subscribe to footstep events for hoof sounds
        SubscribeLocalEvent<SwineTraitsComponent, GetFootstepSoundEvent>(OnGetFootstepSound);
    }

    private void OnFoodIngested(EntityUid uid, SwineTraitsComponent swine, ref IngestedEvent args)
    {
        // Only apply healing if the food actually provides nutrition
        if (!TryComp<FoodComponent>(args.User, out var food))
            return;

        // Calculate healing amount based on food nutrition value
        var totalReagents = args.Split.Volume;
        var healAmount = (float)totalReagents * swine.FoodHealingMultiplier * 0.02f; // Small multiplier to prevent OP healing
        
        if (healAmount <= 0)
            return;

        // Apply healing as negative damage (healing)
        var healingSpec = new DamageSpecifier();
        healingSpec.DamageDict.Add("Brute", FixedPoint2.New(-healAmount * 0.5f));
        healingSpec.DamageDict.Add("Burn", FixedPoint2.New(-healAmount * 0.3f));
        healingSpec.DamageDict.Add("Airloss", FixedPoint2.New(-healAmount * 0.2f));

        _damageableSystem.TryChangeDamage(uid, healingSpec);
    }

    private void OnDigestibilityCheck(EntityUid uid, SwineTraitsComponent swine, ref IsDigestibleEvent args)
    {
        // Swines can eat anything if the trait is enabled
        if (swine.CanEatAnything)
        {
            args.UniversalDigestion();
        }
    }

    private void OnGetFootstepSound(EntityUid uid, SwineTraitsComponent swine, ref GetFootstepSoundEvent args)
    {
        // Use hoof sounds for swines
        args.Sound = new SoundCollectionSpecifier("FootstepHoof");
    }
}