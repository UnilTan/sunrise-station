using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Species.Swine;

/// <summary>
/// Component to track swine-specific traits and modifiers
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwineTraitsComponent : Component
{
    /// <summary>
    /// Multiplier for healing received from food consumption
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FoodHealingMultiplier = 1.5f;
    
    /// <summary>
    /// Whether the swine can eat any edible items regardless of species restrictions
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanEatAnything = true;
    
    /// <summary>
    /// Multiplier for doafter times
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DoAfterMultiplier = 1.1f;
    
    /// <summary>
    /// Accuracy penalty for ranged attacks (fat fingers)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RangedAccuracyPenalty = 0.9f;
    
    /// <summary>
    /// Screen shake reduction from shooting (fat fingers)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ScreenShakeReduction = 0.7f;
    
    /// <summary>
    /// Stun time multiplier when slipping on soap
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoapStunMultiplier = 1.3f;
}