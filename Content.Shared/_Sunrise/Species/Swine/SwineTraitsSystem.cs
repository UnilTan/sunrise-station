using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition;

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
}