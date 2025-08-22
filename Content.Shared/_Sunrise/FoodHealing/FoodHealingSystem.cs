using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;

namespace Content.Shared._Sunrise.FoodHealing;

/// <summary>
/// System that handles healing from food consumption for any entity with FoodHealingComponent
/// </summary>
public sealed class FoodHealingSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to food ingestion events to provide healing
        SubscribeLocalEvent<FoodHealingComponent, IngestedEvent>(OnFoodIngested);
    }

    private void OnFoodIngested(EntityUid uid, FoodHealingComponent foodHealing, ref IngestedEvent args)
    {
        // Only apply healing if the food actually provides nutrition
        if (!TryComp<FoodComponent>(args.User, out var food))
            return;

        // Calculate healing amount based on food nutrition value
        var totalReagents = args.Split.Volume;
        var healAmount = (float)totalReagents * foodHealing.HealingMultiplier * foodHealing.BaseHealingRate;
        
        if (healAmount <= 0)
            return;

        // Apply healing as negative damage (healing) based on distribution
        var healingSpec = new DamageSpecifier();
        
        foreach (var (damageType, percentage) in foodHealing.HealingDistribution)
        {
            var typeHealAmount = healAmount * percentage;
            healingSpec.DamageDict.Add(damageType, FixedPoint2.New(-typeHealAmount));
        }

        _damageableSystem.TryChangeDamage(uid, healingSpec);
    }
}