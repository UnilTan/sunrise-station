using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.FoodHealing;

/// <summary>
/// Component that allows entities to heal from consuming food
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FoodHealingComponent : Component
{
    /// <summary>
    /// Multiplier for healing received from food consumption
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HealingMultiplier = 1.5f;
    
    /// <summary>
    /// Base healing amount per unit of reagent volume
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseHealingRate = 0.02f;
    
    /// <summary>
    /// Distribution of healing across damage types (should sum to 1.0)
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, float> HealingDistribution = new()
    {
        {"Brute", 0.5f},
        {"Burn", 0.3f}, 
        {"Airloss", 0.2f}
    };
}